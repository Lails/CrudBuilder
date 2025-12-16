using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder;

/// <summary>
/// Реализация <see cref="ICrudBuilder"/>, отвечающая за создание команд и запросов
/// и выполнение операций в транзакции с поддержкой retry-механизма.
/// </summary>
/// <typeparam name="TDbContext">Тип контекста базы данных, наследник <see cref="DbContext"/>.</typeparam>
public class CrudBuilder<TDbContext> : ICrudBuilder
    where TDbContext : DbContext
{
    /// <summary>
    /// Провайдер сервисов DI-контейнера.
    /// </summary>
    private readonly IServiceProvider _services;

    /// <summary>
    /// Текущий экземпляр контекста базы данных.
    /// </summary>
    private readonly TDbContext _dbContext;

    /// <summary>
    /// Создает новый экземпляр <see cref="CrudBuilder{TDbContext}"/>.
    /// </summary>
    /// <param name="dbContext">Экземпляр контекста базы данных.</param>
    /// <param name="services">Провайдер сервисов для разрешения зависимостей.</param>
    public CrudBuilder(TDbContext dbContext, IServiceProvider services)
    {
        _services = services;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public TQuery BuildQuery<TQuery>()
        where TQuery : BaseQuery
    {
        var tQuery = _services.GetRequiredService<TQuery>();

        tQuery
            .SetDbContext(_dbContext);

        return tQuery;
    }

    /// <inheritdoc />
    public TCommand BuildCommand<TCommand>()
        where TCommand : BaseCommand
    {
        var tCommand = _services.GetRequiredService<TCommand>();

        tCommand
            .SetDbContext(_dbContext);

        return tCommand;
    }

    /// <inheritdoc />
    public async Task<TResult> WithTransaction<TResult>(Func<Task<TResult>> func,
        IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
        uint retryCount = 1,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            func,
            isolationLevel,
            retryCount,
            retryDelay,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task WithTransaction(Func<Task> func,
        IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
        uint retryCount = 1,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);

        await ExecuteWithRetryAsync(
            async () => { await func(); return true; },
            isolationLevel,
            retryCount,
            retryDelay,
            cancellationToken);
    }

    /// <summary>
    /// Внутренний вспомогательный метод, выполняющий указанное действие в транзакции
    /// с поддержкой повторных попыток при возникновении <see cref="System.Data.DBConcurrencyException"/>.
    /// </summary>
    /// <typeparam name="TResult">Тип результата, возвращаемого действием.</typeparam>
    /// <param name="action">Асинхронное действие, выполняемое в транзакции.</param>
    /// <param name="isolationLevel">Уровень изоляции транзакции.</param>
    /// <param name="retryCount">Количество попыток при конфликте параллелизма.</param>
    /// <param name="retryDelay">Базовая задержка между повторными попытками.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    private static async Task<TResult> ExecuteWithRetryAsync<TResult>(
        Func<Task<TResult>> action,
        IsolationLevel isolationLevel,
        uint retryCount,
        TimeSpan? retryDelay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (retryCount <= 0)
        {
            throw new ArgumentException($"{nameof(retryCount)} должен быть больше 0");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Быстрый путь для retryCount=1 (нет retry)
        if (retryCount == 1)
        {
            var transactionOptions = new TransactionOptions { IsolationLevel = isolationLevel };
            using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

            var result = await action();
            scope.Complete();
            return result;
        }

        // Полная retry-логика для retryCount > 1
        var baseDelay = retryDelay ?? TimeSpan.FromSeconds(0.1);
        var transactionOptionsRetry = new TransactionOptions { IsolationLevel = isolationLevel };
        using var scopeRetry = new TransactionScope(TransactionScopeOption.Required, transactionOptionsRetry, TransactionScopeAsyncFlowOption.Enabled);

        uint retryIterator = 0;
        while (retryCount > retryIterator)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await action();

                scopeRetry.Complete();

                return result;
            }
            catch (System.Data.DBConcurrencyException)
            {
                retryIterator++;

                if (retryIterator >= retryCount)
                {
                    throw;
                }
                else
                {
                    // Exponential backoff: увеличиваем задержку с каждой попыткой
                    var exponentialDelay = TimeSpan.FromMilliseconds(
                        baseDelay.TotalMilliseconds * Math.Pow(2, retryIterator - 1));
                    await Task.Delay(exponentialDelay, cancellationToken);
                }
            }
            catch { throw; }
        }

        // Этот код теоретически недостижим
        throw new InvalidOperationException($"Unexpected end of retry loop. Retry: {retryIterator}/{retryCount}. This indicates a logic error in retry mechanism.");
    }
}
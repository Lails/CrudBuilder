using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder;

/// <summary>
/// Базовый класс для реализаций <see cref="ICrudBuilder"/>
/// </summary>
public abstract class BaseCrudBuilder
{
    /// <summary>
    /// Выполняет указанное действие в транзакции с поддержкой повторных попыток
    /// при возникновении <see cref="System.Data.DBConcurrencyException"/>.
    /// </summary>
    /// <typeparam name="TResult">Тип результата, возвращаемого действием.</typeparam>
    /// <param name="action">Асинхронное действие, выполняемое в транзакции.</param>
    /// <param name="isolationLevel">Уровень изоляции транзакции.</param>
    /// <param name="retryCount">Количество попыток при конфликте параллелизма.</param>
    /// <param name="retryDelay">Базовая задержка между повторными попытками.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    protected static async Task<TResult> ExecuteWithRetryAsync<TResult>(
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
            catch
            {
                throw;
            }
        }

        // Этот код теоретически недостижим
        throw new InvalidOperationException($"Неожиданное завершение цикла повторных попыток. Retry: {retryIterator}/{retryCount}. Это указывает на логическую ошибку в механизме повторов.");
    }
}



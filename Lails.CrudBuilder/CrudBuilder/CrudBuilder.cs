using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder;

/// <summary>
/// Реализация <see cref="ICrudBuilder"/>, отвечающая за создание команд и запросов
/// и выполнение операций в транзакции с поддержкой retry-механизма.
/// </summary>
/// <typeparam name="TDbContext">Тип контекста базы данных, наследник <see cref="DbContext"/>.</typeparam>
public class CrudBuilder<TDbContext> : BaseCrudBuilder, ICrudBuilder
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

}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder;

/// <summary>
/// Реализация <see cref="ICrudBuilder"/> с разделением контекста для чтения и записи.
/// Запросы используют <typeparamref name="TReadDbContext"/>, команды — <typeparamref name="TWriteDbContext"/>.
/// </summary>
/// <typeparam name="TReadDbContext">Тип контекста для операций чтения.</typeparam>
/// <typeparam name="TWriteDbContext">Тип контекста для операций записи.</typeparam>
public class CrudBuilder<TReadDbContext, TWriteDbContext> : BaseCrudBuilder, ICrudBuilder
    where TReadDbContext : DbContext
    where TWriteDbContext : DbContext
{
    private readonly IServiceProvider _services;
    private readonly TReadDbContext _readDbContext;
    private readonly TWriteDbContext _writeDbContext;

    /// <summary>
    /// Создает новый экземпляр <see cref="CrudBuilder{TReadDbContext, TWriteDbContext}"/>.
    /// </summary>
    /// <param name="readDbContext">Контекст для операций чтения.</param>
    /// <param name="writeDbContext">Контекст для операций записи.</param>
    /// <param name="services">Провайдер сервисов для разрешения зависимостей.</param>
    public CrudBuilder(
        TReadDbContext readDbContext,
        TWriteDbContext writeDbContext,
        IServiceProvider services)
    {
        _services = services;
        _readDbContext = readDbContext;
        _writeDbContext = writeDbContext;
    }

    /// <inheritdoc />
    public TQuery BuildQuery<TQuery>()
        where TQuery : BaseQuery
    {
        var tQuery = _services.GetRequiredService<TQuery>();

        tQuery
            .SetDbContext(_readDbContext);

        return tQuery;
    }

    /// <inheritdoc />
    public TCommand BuildCommand<TCommand>()
        where TCommand : BaseCommand
    {
        var tCommand = _services.GetRequiredService<TCommand>();

        tCommand
            .SetDbContext(_writeDbContext);

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



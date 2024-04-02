using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder;

public class CrudBuilder<TDbContext> : ICrudBuilder
    where TDbContext : DbContext
{
    readonly IServiceProvider _services;
    readonly TDbContext _dbContext;

    private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

    public CrudBuilder(TDbContext dbContext, IServiceProvider services)
    {
        _services = services;
        _dbContext = dbContext;
    }

    public TQuery BuildQuery<TQuery>()
        where TQuery : BaseQuery
    {
        var tQuery = _services.GetService<TQuery>();
        if (tQuery == null)
        {
            throw new NullReferenceException(nameof(tQuery));
        }

        tQuery
            .SetDbContext(_dbContext);

        return tQuery;
    }

    public TCommand BuildCommand<TCommand>()
        where TCommand : BaseCommand
    {
        var tCommand = _services.GetService<TCommand>();
        if (tCommand == null)
        {
            throw new NullReferenceException(nameof(tCommand));
        }

        tCommand
            .SetDbContext(_dbContext);

        return tCommand;
    }
    public async Task<TResult> WithTransaction<TResult>(Func<Task<TResult>> func, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        //await semaphoreSlim.WaitAsync();
        var transactionOptions = new TransactionOptions { IsolationLevel = isolationLevel };
        using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions);

        try
        {
            var result = func().ConfigureAwait(true).GetAwaiter().GetResult();

            scope.Complete();

            return result;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
       //     semaphoreSlim.Release();
        }
    }

    public async Task WithTransaction(Func<Task> func, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
       // await semaphoreSlim.WaitAsync();
        var transactionOptions = new TransactionOptions { IsolationLevel = isolationLevel };
        using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions);

        try
        {
            func().ConfigureAwait(true).GetAwaiter().GetResult();

            scope.Complete();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
      //      semaphoreSlim.Release();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder;

public class CrudBuilder<TDbContext> : ICrudBuilder
    where TDbContext : DbContext
{
    readonly IServiceProvider _services;
    readonly TDbContext _dbContext;

    public CrudBuilder(TDbContext dbContext, IServiceProvider services)
    {
        _services = services;
        _dbContext = dbContext;
    }

    public TQuery BuildQuery<TQuery>()
        where TQuery : BaseQuery
    {
        var tQuery = _services.GetRequiredService<TQuery>();

        tQuery
            .SetDbContext(_dbContext);

        return tQuery;
    }

    public TCommand BuildCommand<TCommand>()
        where TCommand : BaseCommand
    {
        var tCommand = _services.GetRequiredService<TCommand>();

        tCommand
            .SetDbContext(_dbContext);

        return tCommand;
    }

    public async Task<TResult> WithTransaction<TResult>(Func<Task<TResult>> func,
        IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
        uint retryCount = 1)
    {
        var transactionOptions = new TransactionOptions { IsolationLevel = isolationLevel };
        using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

        uint retryIterator = 0;
        while (retryCount > retryIterator)
        {
            try
            {
                var result = await func();

                scope.Complete();

                return result;
            }
            catch (System.Data.DBConcurrencyException ex)
            {
                Console.WriteLine($"Retried:{retryIterator} message:{ex}. stack:{ex.StackTrace}. er:{ex.Message}");
                retryIterator++;

                if (retryCount < retryIterator)
                {
                    throw;
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                }
            }
            catch { throw; }
        }

        throw new InvalidOperationException();
    }

    public async Task WithTransaction(Func<Task> func,
        IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
        uint retryCount = 1)
    {
        var transactionOptions = new TransactionOptions { IsolationLevel = isolationLevel };
        using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

        uint retryIterator = 0;
        while (retryCount > retryIterator)
        {
            try
            {
                await func();

                scope.Complete();
                break;
            }
            catch (System.Data.DBConcurrencyException ex)
            {
                Console.WriteLine($"Retried:{retryIterator} message:{ex}. stack:{ex.StackTrace}. er:{ex.Message}");
                retryIterator++;

                if (retryCount < retryIterator)
                {
                    throw;
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                }
            }
            catch { throw; }
        }
    }
}
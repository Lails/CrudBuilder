using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder
{
    public interface ICrudBuilder
    {
        TQuery BuildQuery<TQuery>()
            where TQuery : BaseQuery;

        TCommand BuildCommand<TCommand>()
            where TCommand : BaseCommand;

        Task<TResult> WithTransaction<TResult>(
            Func<Task<TResult>> func,
            IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
            uint retryCount = 1
            );

        Task WithTransaction(Func<Task> func,
            IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
            uint retryCount = 1
            );
    }
}

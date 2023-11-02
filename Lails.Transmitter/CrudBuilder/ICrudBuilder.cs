using Microsoft.EntityFrameworkCore;

namespace Lails.Transmitter.CrudBuilder
{
    public interface ICrudBuilder<TDbContext> where TDbContext : DbContext
    {
        TQuery BuildQuery<TQuery>()
            where TQuery : BaseQuery;

        TCommand BuildCommand<TCommand>()
            where TCommand : BaseCommand;
    }
}

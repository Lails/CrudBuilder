using Microsoft.EntityFrameworkCore;

namespace Lails.CrudBuilder.CrudBuilder
{
    public interface ICrudBuilder
    {
        TQuery BuildQuery<TQuery>()
            where TQuery : BaseQuery;

        TCommand BuildCommand<TCommand>()
            where TCommand : BaseCommand;
    }
}

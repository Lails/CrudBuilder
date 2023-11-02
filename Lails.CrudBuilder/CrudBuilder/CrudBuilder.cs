using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Lails.CrudBuilder.CrudBuilder
{
    public class CrudBuilder<TDbContext> : ICrudBuilder<TDbContext>
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
            var instance = _services.GetService<TQuery>();
            instance.SetDbContext(_dbContext);
            return instance;
        }

        public TCommand BuildCommand<TCommand>() 
            where TCommand : BaseCommand
        {
            var instance = _services.GetService<TCommand>();
            instance.SetDbContext(_dbContext);
            return instance;
        }
    }
}
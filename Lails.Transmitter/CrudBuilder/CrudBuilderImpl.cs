using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Lails.Transmitter.CrudBuilder
{
    public class CrudBuilderImpl<TDbContext> : CrudBuilder<TDbContext>
        where TDbContext : DbContext
    {
        //readonly IDbCrud<TDbContext> _dbCRUD;
        readonly IServiceProvider _services;
        readonly TDbContext _dbContext;
        public CrudBuilderImpl(TDbContext dbContext, IServiceProvider services)
        {
            //_dbCRUD = new DbCRUD<TDbContext>(dbContext);
            _services = services;
            _dbContext = dbContext;
        }


        public TQuery BuildQuery<TQuery>()
            where TQuery : BaseQuery
        {
            var instance = _services.GetService<TQuery>();
            instance.SetDbContext(_dbContext);
            return instance;

            //TQuery instance = Activator.CreateInstance<TQuery>();
            //instance.GetType().BaseType.BaseType.GetField(BaseCrudOperations<TDbContext>.DbCrudFieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, _dbCRUD);
            //return instance;
        } 
    }
}
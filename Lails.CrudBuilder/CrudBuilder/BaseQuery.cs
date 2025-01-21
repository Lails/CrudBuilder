using Microsoft.EntityFrameworkCore;

namespace Lails.CrudBuilder.CrudBuilder
{
    public abstract class BaseQuery
    {
        private DbContext _db = null!;
        internal void SetDbContext<TDbContext>(TDbContext db)
            where TDbContext : DbContext
        {
            _db = db;
        }

        protected IQueryable<TEntity> GetAsNoTracking<TEntity>()
            where TEntity : class
        {
            return _db.Set<TEntity>().AsNoTracking();
        }

        protected IQueryable<TEntity> GetAsTracking<TEntity>()
            where TEntity : class
        {
            return _db.Set<TEntity>().AsTracking();
        }

    }

    //public abstract class BaseQuery<TEntity, TFilter, TDbContext> : BaseCrudOperations<TDbContext>
    //	where TEntity : class
    //	where TDbContext : DbContext
    //	where TFilter : IQueryFilter
    //{
    //	protected IQueryable<TEntity> Query { get; }

    //	public abstract IQueryable<TEntity> QueryDefinition(ref IQueryable<TEntity> query);
    //	public abstract IQueryable<TEntity> QueryFilter(ref IQueryable<TEntity> query, TFilter filter);

    //	internal TFilter Filter { get; private set; }
    //	internal bool IsAsNoTracking { get; private set; }

    //	public async Task<List<TEntity>> ApplyFilterAsNoTraking(TFilter filter)
    //	{
    //		IsAsNoTracking = true;

    //		return await ApplyFilter(filter);
    //	}
    //	public async Task<List<TEntity>> ApplyFilter(TFilter filter)
    //	{
    //		Filter = filter;

    //		return await _dbCrud.GetByFilterAsync(this);
    //	}
    //}
}


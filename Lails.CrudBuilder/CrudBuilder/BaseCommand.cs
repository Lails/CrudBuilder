using Microsoft.EntityFrameworkCore;

namespace Lails.CrudBuilder.CrudBuilder
{
    public abstract class BaseCommand
    {
        private DbContext _db = null!;
        internal void SetDbContext<TDbContext>(TDbContext db)
            where TDbContext : DbContext
        {
            _db = db;
        }

        protected DbSet<TEntity> GetSet<TEntity>()
            where TEntity : class
        {
            return _db.Set<TEntity>();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }

        public int SaveChanges()
        {
            return _db.SaveChanges();
        }

        public int SaveChanges(bool acceptAllChangesOnSuccess)
        {

            return _db.SaveChanges(acceptAllChangesOnSuccess);
        }
    }




    //public abstract class BaseCommand<TData> : BaseCrudOperations
    //    where TData : class
    //{
    //    public async Task Execute(TData data)
    //    {
    //        await BeforeCreateAsync(data);

    //        await _dbCrud.CreateAsync(data);

    //        await AfterCreateAsync(data);
    //    }

    //    public abstract Task BeforeCreateAsync(TData data);
    //    public abstract Task AfterCreateAsync(TData data);
    //    protected virtual async Task ChangeTrackerAsync(/*TODO:*/) { }
    //}
}
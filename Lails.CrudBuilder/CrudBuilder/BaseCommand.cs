using Microsoft.EntityFrameworkCore;

namespace Lails.CrudBuilder.CrudBuilder
{
    /// <summary>
    /// Базовый класс для всех CQRS-команд, работающих с <see cref="DbContext"/>.
    /// Предоставляет доступ к наборам сущностей и стандартным методам сохранения изменений.
    /// </summary>
    public abstract class BaseCommand
    {
        private DbContext _db = null!;

        /// <summary>
        /// Устанавливает текущий <see cref="DbContext"/> для команды.
        /// Вызывается инфраструктурой <see cref="CrudBuilder{TDbContext}"/>.
        /// </summary>
        internal void SetDbContext<TDbContext>(TDbContext db)
            where TDbContext : DbContext
        {
            _db = db;
        }

        /// <summary>
        /// Возвращает <see cref="DbSet{TEntity}"/> для указанного типа сущности.
        /// </summary>
        /// <typeparam name="TEntity">Тип сущности.</typeparam>
        protected DbSet<TEntity> GetSet<TEntity>()
            where TEntity : class
        {
            return _db.Set<TEntity>();
        }

        /// <summary>
        /// Асинхронно сохраняет изменения в базе данных.
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Асинхронно сохраняет изменения в базе данных с поддержкой отмены.
        /// </summary>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Синхронно сохраняет изменения в базе данных.
        /// </summary>
        public int SaveChanges()
        {
            return _db.SaveChanges();
        }

        /// <summary>
        /// Синхронно сохраняет изменения в базе данных с управлением применением всех изменений.
        /// </summary>
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
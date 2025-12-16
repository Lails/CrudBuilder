using Microsoft.EntityFrameworkCore;

namespace Lails.CrudBuilder.CrudBuilder
{
    /// <summary>
    /// Базовый класс для всех CQRS-запросов, работающих с <see cref="DbContext"/>.
    /// Предоставляет вспомогательные методы для получения данных с/без трекинга.
    /// </summary>
    public abstract class BaseQuery
    {
        private DbContext _db = null!;

        /// <summary>
        /// Устанавливает текущий <see cref="DbContext"/> для запроса.
        /// Вызывается инфраструктурой <see cref="CrudBuilder{TDbContext}"/>.
        /// </summary>
        internal void SetDbContext<TDbContext>(TDbContext db)
            where TDbContext : DbContext
        {
            _db = db;
        }

        /// <summary>
        /// Возвращает запрос к сущностям <typeparamref name="TEntity"/> без трекинга изменений.
        /// Подходит для операций только на чтение.
        /// </summary>
        protected IQueryable<TEntity> GetAsNoTracking<TEntity>()
            where TEntity : class
        {
            return _db.Set<TEntity>().AsNoTracking();
        }

        /// <summary>
        /// Возвращает запрос к сущностям <typeparamref name="TEntity"/> с трекингом изменений.
        /// Подходит для сценариев, где сущности будут изменяться и сохраняться.
        /// </summary>
        protected IQueryable<TEntity> GetAsTracking<TEntity>()
            where TEntity : class
        {
            return _db.Set<TEntity>().AsTracking();
        }

        /// <summary>
        /// Потоковое чтение сущностей без трекинга через <see cref="IAsyncEnumerable{T}"/>.
        /// Подходит для больших выборок, чтобы не загружать все данные в память.
        /// </summary>
        protected async IAsyncEnumerable<TEntity> GetAsNoTrackingStream<TEntity>()
            where TEntity : class
        {
            await foreach (var item in _db.Set<TEntity>().AsNoTracking().AsAsyncEnumerable())
            {
                yield return item;
            }
        }

        /// <summary>
        /// Потоковое чтение сущностей с трекингом через <see cref="IAsyncEnumerable{T}"/>.
        /// Использовать осторожно для очень больших выборок из-за роста ChangeTracker.
        /// </summary>
        protected async IAsyncEnumerable<TEntity> GetAsTrackingStream<TEntity>()
            where TEntity : class
        {
            await foreach (var item in _db.Set<TEntity>().AsTracking().AsAsyncEnumerable())
            {
                yield return item;
            }
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


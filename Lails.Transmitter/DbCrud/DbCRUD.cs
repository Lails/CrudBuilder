//using Lails.Transmitter.CrudBuilder;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Lails.Transmitter.DbCrud
//{
//    internal sealed class DbCRUD<TDbContext> : IDbCrud<TDbContext> where TDbContext : DbContext
//	{
//		readonly TDbContext _context;
//		public DbCRUD(TDbContext context)
//		{
//			_context = context;
//			if (_context == null)
//			{
//				throw new NullReferenceException($"DbContext is null.");
//			}
//		}

//		public async Task CreateAsync<TData>(TData data) where TData : class
//		{
//			if (data == null)
//			{
//				throw new ArgumentNullException(nameof(data));
//			}

//			if (data is IEnumerable enities)
//			{
//				foreach (var enity in enities)
//				{
//					await _context.AddAsync(enity);
//				}
//			}
//			else
//			{
//				await _context.AddAsync(data);
//			}
//			await _context.SaveChangesAsync();
//		}

//		public async Task UpdateAsync<TData>(TData data) where TData : class
//		{
//			if (data is IEnumerable enities)
//			{
//				foreach (var enity in enities)
//				{
//					_context.Update(enity);
//				}
//			}
//			else
//			{
//				_context.Update(data);
//			}
//			await _context.SaveChangesAsync();
//		}

//		public async Task DeleteAsync<TData>(TData data) where TData : class
//		{
//			if (data is IEnumerable enities)
//			{
//				foreach (var enity in enities)
//				{
//					_context.Remove(enity);
//				}
//			}
//			else
//			{
//				_context.Remove(data);
//			}
//			await _context.SaveChangesAsync();
//		}



//		public async Task<List<TEntity>> GetByFilterAsync<TEntity, TFilter>(BaseQuery<TEntity, TFilter, TDbContext> definedQuery)
//			where TEntity : class
//			where TFilter : IQueryFilter
//		{
//			var query = _context.Set<TEntity>().AsQueryable();

//			definedQuery.QueryDefinition(ref query);
//			if (definedQuery.Filter != null)
//			{
//				definedQuery.QueryFilter(ref query, definedQuery.Filter);
//			}

//			query = definedQuery.IsAsNoTracking
//				? query.AsNoTracking()
//				: query.AsTracking();

//			var result = await query.ToListAsync();
//			return result;
//		}
//	}
//}

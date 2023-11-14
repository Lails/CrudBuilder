//using Lails.DBContext;
//using Lails.CrudBuilder.CrudBuilder;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Linq;

//namespace Lails.CrudBuilder.Load.Tetst.BusinessLogic.Queries
//{
//    public class CustomerQuery : BaseQuery
//	{
//		public override IQueryable<Customer> QueryDefinition(ref IQueryable<Customer> query)
//		{
//			return query
//				.Include(r=>r.Invoices); 
//		}

//		public override IQueryable<Customer> QueryFilter(ref IQueryable<Customer> query, CustomerFilter filter)
//		{
//			if (filter.Id.HasValue)
//			{
//				query = query.Where(r => r.Id == filter.Id);
//			}

//			return query;
//		}
//	}

//	public class CustomerFilter : IQueryFilter
//	{
//		public Guid? Id { get; set; }
//	}

//}

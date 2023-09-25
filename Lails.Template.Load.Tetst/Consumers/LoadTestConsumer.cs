//using Lails.DBContext;
//using Lails.Template.Load.Tetst.BusinessLogic.Commands;
//using Lails.Template.Load.Tetst.BusinessLogic.Queries;
//using Lails.Transmitter.CrudBuilder;
//using MassTransit;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Lails.Template.Load.Tetst.Consumers
//{
//	public class LoadTestConsumer : IConsumer<ILoadTestEvent>
//	{
//		readonly ICrudBuilder<LailsDbContext> _crudBuilder;
//		readonly LailsDbContext _lailsDbContext;
//		public LoadTestConsumer(ICrudBuilder<LailsDbContext> cRUDBuilder, LailsDbContext lailsDbContext)
//		{
//			_crudBuilder = cRUDBuilder;
//			_lailsDbContext = lailsDbContext;
//		}
//		public async Task Consume(ConsumeContext<ILoadTestEvent> context)
//		{
//			var customer = new Customer
//			{
//				FirstName = "Elizabeth",
//				LastName = "Lincoln",
//				Address = "23 Tsawassen Blvd.",
//				Invoices = new List<Invoice> { new Invoice { Date = DateTime.UtcNow } }
//			};

//			await _crudBuilder.Build<CustomerCreate>().Execute(customer);


//			CustomerFilter filter = new CustomerFilter { Id = customer.Id };
//			var r = await _crudBuilder.Build<CustomerQuery>().ApplyFilter(filter);


//			var r2 = await _lailsDbContext.Set<Customer>().AsQueryable().Where(r => r.Id == customer.Id).AsTracking().ToListAsync();

//			//Console.WriteLine(customer.Id);
//		}
//	}

//	public interface ILoadTestEvent { }
//}

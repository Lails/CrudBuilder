using Lails.CrudBuilder.CrudBuilder;
using Lails.CrudBuilder.DBContext;
using Lails.CrudBuilder.Tests.BusinessLogic.Commands;
using Lails.MQ.Rabbit.Consumer;
using MassTransit;

namespace Lails.CrudBuilder.Load.Tetst.Consumers
{
    public class LoadTestConsumer : BaseConsumer<ILoadTestEvent>
    {
        readonly ICrudBuilder _crudBuilder;
        readonly LailsDbContext dBContext;
        public LoadTestConsumer(ICrudBuilder crudBuilder,
            LailsDbContext lailsDbContext)
        {
            _crudBuilder = crudBuilder;
            dBContext = lailsDbContext;
        }
        protected override async Task ConsumeImplementation(ConsumeContext<ILoadTestEvent> context)
        {
            //var intnum = 24_00;
            //var intnum2 = 234 % 100;
            //var guid = Guid.NewGuid();
            //IEnumerable<Invoice> _invoices = dBContext.Invoices;
            //_invoices = _invoices.Where(r => r.Id == guid);
            //var res = _invoices.ToArray();
            //var i = res[0];

            //try
            //{
            //    //var t = Task.Run(() =>
            //    //{
            //    //    Thread.Sleep(100);
            //    //    throw new Exception("dfd");
            //    //});

            //    await Task.WhenAll();
            //}
            //catch (Exception ex)
            //{

            //    throw;
            //}
            

            await TranasktionTests();

            var customer = new Customer
            {
                FirstName = "Elizabeth",
                LastName = "Lincoln",
                Address = "23 Tsawassen Blvd.",
                Invoices = new List<Invoice> { new Invoice { Date = DateTime.UtcNow } }
            };

            await _crudBuilder.BuildCommand<CustomerCommands>().Create(customer);


            //CustomerFilter filter = new CustomerFilter { Id = customer.Id };
            //var r = await _crudBuilder.BuildQuery<CustomerQuery>().GetByFilterAsNoTracking(filter);


            //var r2 = await dBContext.Set<Customer>().AsQueryable().Where(r => r.Id == customer.Id).AsTracking().ToListAsync();

        }

        private async Task TranasktionTests()
        {
            var newCustomer = new Customer { FirstName = "Elizabeth", LastName = "Lincoln", Address = "23 Tsawassen Blvd.", };
            Guid customertId = Guid.Empty;

            try
            {
                customertId = await _crudBuilder.WithTransaction(async () =>
                {
                    customertId = await _crudBuilder.BuildCommand<CustomerCommands>().Create(newCustomer);
                    await _crudBuilder.BuildCommand<CustomerCommands>().Update(newCustomer);
                    return newCustomer.Id;
                }, System.Transactions.IsolationLevel.Serializable);
            }
            catch (Exception)
            {
                var existingCustomer = dBContext.Customers.SingleOrDefault(r => r.Id == customertId);
            }
        }
    }

    public interface ILoadTestEvent { }
}

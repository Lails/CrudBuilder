using Lails.CrudBuilder.CrudBuilder;
using Lails.CrudBuilder.DBContext;
using Lails.CrudBuilder.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lails.CrudBuilder.Tests
{

    public class Setup
    {
        protected LailsDbContext Context;
        protected ICrudBuilder CrudBuilder;

        [OneTimeSetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();


            services
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<LailsDbContext>((serviceProvider, options) => options.UseInMemoryDatabase("LailsDbContext").UseInternalServiceProvider(serviceProvider));

            services
                .AddDbCrud<LailsDbContext>()
                .RegisterQueriesAndCommands<Setup, Setup>();

            var provider = services.BuildServiceProvider();


            Context = (LailsDbContext)provider.GetService(typeof(LailsDbContext));
            CrudBuilder = (ICrudBuilder)provider.GetService(typeof(ICrudBuilder));

        }

        [OneTimeTearDown]
        public void TaerDown()
        {
        }


        public readonly static CustomerStruct TestCustomer1 = new() { FirstName = "Angry", LastName = "Birdth", Address = "Sydney" };
        public readonly static CustomerStruct TestCustomer2 = new() { FirstName = "Red", LastName = "Birdth", Address = "Melbourne" };
        public struct CustomerStruct
        {
            public Guid Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Address { get; set; }
        }

        public async Task SeedDatabase()
        {
            await Context.Customers.AddRangeAsync(new[] {
                new Customer { FirstName = TestCustomer1.FirstName, LastName = TestCustomer1.LastName, Address = TestCustomer1.Address },
                new Customer { FirstName = TestCustomer2.FirstName, LastName = TestCustomer2.LastName, Address = TestCustomer2.Address }
            });
            await Context.SaveChangesAsync();
        }

        public async Task ResetDatabase()
        {
            var invoces = await Context.Invoices.ToListAsync();
            Context.Invoices.RemoveRange(invoces);
            await Context.SaveChangesAsync();

            var customers = await Context.Customers.ToListAsync();
            Context.Customers.RemoveRange(customers);
            await Context.SaveChangesAsync();
        }
    }
}

using Lails.DBContext;
using Lailts.Transmitter.Tests.BusinessLogic.Queries;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lailts.Transmitter.Tests
{
	public class CrudBuilderQueriesTests : Setup
	{
		[SetUp]
		public void Setup()
		{
			SeedDatabase().Wait();
		}
		[TearDown]
		public void Down()
		{
			ResetDatabase().Wait();
		}

		[Test]
		public async Task Retriever_GetElementById_RetrunsOneElement()
		{
			var customer = new Customer
			{
				Id = Guid.NewGuid(),
				FirstName = "Sarah",
				LastName = "Conor",
				Address = "Sydney",
				Invoices = new List<Invoice> { new Invoice { Date = DateTime.UtcNow } }
			};
			Context.Customers.Add(customer);
			Context.SaveChanges();

			var filter = CustomerFilter.Create()
				.SetId(customer.Id);
			List<Customer> customersResult = await CrudBuilder.BuildQuery<CustomerQuery>().GetByFilter(filter);
			var oneCustomer = customersResult.Single();

            Assert.AreEqual(customer.Id, oneCustomer.Id);
            Assert.AreEqual(customer.Invoices.Count, oneCustomer.Invoices.Count);
		}

		[TestCase("Angry", 1)]
		[TestCase(null, 2)]
		[Test]
		public void Retriever_GetElementsByFilter_RetrunsElementsExpectedCount(string firstName, int expectedCount)
		{
			var filter = CustomerFilter.Create()
				.SetfirstName(firstName);
			List<Customer> customersResult = CrudBuilder.BuildQuery<CustomerQuery>().GetByFilter(filter).Result;

			Assert.AreEqual(expectedCount, customersResult.Count);
		}

		[Test]
		public void Retriever_GetAllElementsByFilterNull_RetrunsOneElement()
		{
			List<Customer> customersResult = CrudBuilder.BuildQuery<CustomerQuery>().GetByFilter(null).Result;

			Assert.AreEqual(Context.Customers.Count(), customersResult.Count);
		}

		[Test]
		public void Retriever_CheckTracking_ReturnsSuccessChanges()
		{
			var newCustomer = new Customer { FirstName = MethodBase.GetCurrentMethod().Name };
			Context.Add(newCustomer);
			Context.SaveChanges();

			var filter = CustomerFilter.Create()
				.SetId(newCustomer.Id);
			Customer customer = CrudBuilder.BuildQuery<CustomerQuery>().GetByFilter(filter).Result.Single();
			customer.FirstName += "_changed";
			Context.SaveChanges();

			var reloadedCustomer = Context.Customers.FirstOrDefault(r => r.Id == newCustomer.Id);
			Assert.AreEqual(reloadedCustomer.FirstName, MethodBase.GetCurrentMethod().Name + "_changed");
		}
		[Test]
		public void RetrieverAsNoTracking_CheckNoTracking_ReturnsNoChanges()
		{
			var newCustomer = new Customer { FirstName = MethodBase.GetCurrentMethod().Name };
			Context.Add(newCustomer);
			Context.SaveChanges();

			var filter = CustomerFilter.Create()
				.SetId(newCustomer.Id);
			Customer customer = CrudBuilder.BuildQuery<CustomerQuery>().GetByFilterAsNoTracking(filter).Result.Single();
			customer.FirstName += "_changed";
			Context.SaveChanges();

			var reloadedCustomer = Context.Customers.FirstOrDefault(r => r.Id == newCustomer.Id);
			Assert.AreEqual(reloadedCustomer.FirstName, MethodBase.GetCurrentMethod().Name);
		}
		//TODO: Add test, what will write AsNotraking in QueryDefinition
	}
}
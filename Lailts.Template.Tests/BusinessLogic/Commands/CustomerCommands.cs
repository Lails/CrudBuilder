using Lails.DBContext;
using Lails.Transmitter.CrudBuilder;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lailts.Transmitter.Tests.BusinessLogic.Commands
{
    public class CustomerCommands : BaseCommand
    {
        public async Task Create(Customer customer)
        {
            GetSet<Customer>().Add(customer);
            await SaveChangesAsync();
        }
        public async Task Create(List<Customer> customers)
        {
            await GetSet<Customer>().AddRangeAsync(customers);
            await SaveChangesAsync();
        }

        public async Task Update(Customer customer)
        {
            GetSet<Customer>().Update(customer);
            await SaveChangesAsync();
        }

        public async Task Update(List<Customer> customers)
        {
            GetSet<Customer>().UpdateRange(customers);
            await SaveChangesAsync();
        }

        public async Task Delete(Customer customer)
        {
            GetSet<Customer>().Remove(customer);
            await SaveChangesAsync();
        }

        public async Task Delete(List<Customer> customers)
        {
            GetSet<Customer>().RemoveRange(customers);
            await SaveChangesAsync();
        }
    }
}

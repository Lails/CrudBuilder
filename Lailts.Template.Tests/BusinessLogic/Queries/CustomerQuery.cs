using Lails.DBContext;
using Lails.Transmitter.CrudBuilder;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lailts.Transmitter.Tests.BusinessLogic.Queries
{
    public class CustomerQuery : BaseQuery
    {
        public async Task<List<Customer>> GetByFilter(CustomerFilter filter)
        {

            var query = GetAsTracking<Customer>();
            query = query
                .Include(r => r.Invoices);

            if (filter != null)
                ApplyFilter(ref query, filter);

            return await query.ToListAsync();
        }
        public async Task<List<Customer>> GetByFilterAsNoTracking(CustomerFilter filter)
        {
            var query = GetAsNoTracking<Customer>();

            ApplyFilter(ref query, filter);

            return await query.ToListAsync();
        }

        private void ApplyFilter(ref IQueryable<Customer> query, CustomerFilter filter)
        {
            if (filter.Id.HasValue)
            {
                query = query.Where(r => r.Id == filter.Id);
            }

            if (string.IsNullOrWhiteSpace(filter.FirstName) == false)
            {
                query = query.Where(r => r.FirstName == filter.FirstName);
            }
        }
    }

    public class CustomerFilter
    {
        public static CustomerFilter Create() => new CustomerFilter();
        public Guid? Id { get; set; }
        public string FirstName { get; set; }

        public CustomerFilter SetId(Guid? id)
        {
            Id = id;
            return this;
        }
        public CustomerFilter SetfirstName(string firstName)
        {
            FirstName = firstName;
            return this;
        }
    }

}

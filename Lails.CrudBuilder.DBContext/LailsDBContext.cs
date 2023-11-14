using Microsoft.EntityFrameworkCore;

namespace Lails.CrudBuilder.DBContext
{
	public class LailsDbContext : DbContext
	{
		public LailsDbContext(DbContextOptions<LailsDbContext> options) : base(options) { }
		public DbSet<Customer> Customers { get; set; }
		public DbSet<Invoice> Invoices { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			 

			modelBuilder.Entity<Customer>().HasKey(r => r.Id);
			modelBuilder.Entity<Invoice>().HasKey(r => r.Id);
		}
	}
}

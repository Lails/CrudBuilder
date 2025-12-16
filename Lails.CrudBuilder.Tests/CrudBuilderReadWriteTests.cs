using Lails.CrudBuilder.CrudBuilder;
using Lails.CrudBuilder.DBContext;
using Lails.CrudBuilder.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Data;
using System.Reflection;

namespace Lails.CrudBuilder.Tests;

public class CrudBuilderReadWriteTests
{
    private class ReadDbContext : DbContext
    {
        public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = null!;
    }

    private class WriteDbContext : DbContext
    {
        public WriteDbContext(DbContextOptions<WriteDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = null!;
    }

    private class TestQuery : BaseQuery { }

    private class TestCommand : BaseCommand { }

    private class CustomerReadQuery : BaseQuery
    {
        public IQueryable<Customer> GetAll()
            => GetAsNoTracking<Customer>();
    }

    private class CustomerWriteCommand : BaseCommand
    {
        public async Task<Guid> Create(Customer customer)
        {
            var set = GetSet<Customer>();
            await set.AddAsync(customer);
            await SaveChangesAsync();
            return customer.Id;
        }
    }

    private static (ReadDbContext read, WriteDbContext write) CreateReadWriteContexts(string readDbName, string writeDbName)
    {
        var readOptions = new DbContextOptionsBuilder<ReadDbContext>()
            .UseInMemoryDatabase(readDbName)
            .Options;
        var writeOptions = new DbContextOptionsBuilder<WriteDbContext>()
            .UseInMemoryDatabase(writeDbName)
            .Options;

        return (new ReadDbContext(readOptions), new WriteDbContext(writeOptions));
    }

    [Test]
    public void AddDbCrud_WithSameReadAndWriteContext_GenericOverload_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<ReadDbContext>(options => options.UseInMemoryDatabase("single"));

        // Act + Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddDbCrud<ReadDbContext, ReadDbContext>();
        });
    }

    [Test]
    public void AddDbCrud_WithDifferentReadAndWriteContexts_UsesSeparatedContextsForQueryAndCommand()
    {
        // Arrange
        var services = new ServiceCollection();

        services
            .AddDbContext<ReadDbContext>(options => options.UseInMemoryDatabase("read"))
            .AddDbContext<WriteDbContext>(options => options.UseInMemoryDatabase("write"))
            .AddDbCrud<ReadDbContext, WriteDbContext>();

        // Регистрируем TestQuery/TestCommand вручную, чтобы не тянуть Scrutor в тесты
        services.AddTransient<TestQuery>();
        services.AddTransient<TestCommand>();

        var provider = services.BuildServiceProvider();

        var crudBuilder = provider.GetRequiredService<ICrudBuilder>();

        // Act
        var query = crudBuilder.BuildQuery<TestQuery>();
        var command = crudBuilder.BuildCommand<TestCommand>();

        var baseQueryDbField = typeof(BaseQuery).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);
        var baseCommandDbField = typeof(BaseCommand).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.That(baseQueryDbField, Is.Not.Null);
        Assert.That(baseCommandDbField, Is.Not.Null);

        var queryContext = baseQueryDbField!.GetValue(query);
        var commandContext = baseCommandDbField!.GetValue(command);

        // Assert
        Assert.That(crudBuilder, Is.TypeOf<CrudBuilder<ReadDbContext, WriteDbContext>>());
        Assert.That(queryContext, Is.TypeOf<ReadDbContext>(), "Query должен использовать read-контекст");
        Assert.That(commandContext, Is.TypeOf<WriteDbContext>(), "Command должен использовать write-контекст");
    }

    [Test]
    public async Task AddDbCrud_WithDifferentReadAndWriteContexts_DataWrittenOnlyToWriteContext()
    {
        // Arrange
        var services = new ServiceCollection();

        services
            .AddDbContext<ReadDbContext>(options => options.UseInMemoryDatabase("read-separation"))
            .AddDbContext<WriteDbContext>(options => options.UseInMemoryDatabase("write-separation"))
            .AddDbCrud<ReadDbContext, WriteDbContext>();

        services.AddTransient<CustomerReadQuery>();
        services.AddTransient<CustomerWriteCommand>();

        var provider = services.BuildServiceProvider();

        var crudBuilder = provider.GetRequiredService<ICrudBuilder>();

        // Act
        var writeCommand = crudBuilder.BuildCommand<CustomerWriteCommand>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "ReadWrite",
            LastName = "Separation",
            Address = "Test"
        };
        await writeCommand.Create(customer);

        var readQuery = crudBuilder.BuildQuery<CustomerReadQuery>();
        var readCustomers = await readQuery.GetAll().ToListAsync();

        // Assert: данные записаны только в write-контекст и не видны через read-контекст
        Assert.That(readCustomers, Is.Empty, "Read-контекст не должен видеть данные, записанные через write-контекст с другой БД");
    }

    [Test]
    public void WithTransaction_ReadWriteBuilder_RetryCount1_ThrowsAfterFirstAttempt()
    {
        // Arrange
        var (read, write) = CreateReadWriteContexts("rw-retry1-read", "rw-retry1-write");
        using var readCtx = read;
        using var writeCtx = write;

        var services = new ServiceCollection().BuildServiceProvider();
        var crudBuilder = new CrudBuilder<ReadDbContext, WriteDbContext>(readCtx, writeCtx, services);

        int attemptCount = 0;

        // Act + Assert
        Assert.ThrowsAsync<DBConcurrencyException>(async () =>
        {
            await crudBuilder.WithTransaction(async () =>
            {
                attemptCount++;
                throw new DBConcurrencyException("test");
            }, retryCount: 1);
        });

        Assert.That(attemptCount, Is.EqualTo(1), "При retryCount=1 должна быть только одна попытка");
    }

    [Test]
    public async Task WithTransaction_ReadWriteBuilder_RetryCount2_SecondAttemptSucceeds()
    {
        // Arrange
        var (read, write) = CreateReadWriteContexts("rw-retry2-read", "rw-retry2-write");
        using var readCtx = read;
        using var writeCtx = write;

        var services = new ServiceCollection().BuildServiceProvider();
        var crudBuilder = new CrudBuilder<ReadDbContext, WriteDbContext>(readCtx, writeCtx, services);

        int attemptCount = 0;

        // Act (первая попытка падает, вторая успешна)
        await crudBuilder.WithTransaction(async () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new DBConcurrencyException("first attempt fails");
            }
        }, retryCount: 2);

        // Assert
        Assert.That(attemptCount, Is.EqualTo(2), "При retryCount=2 должно быть две попытки, вторая успешна");
    }
}



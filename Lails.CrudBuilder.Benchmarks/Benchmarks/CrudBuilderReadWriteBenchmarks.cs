using BenchmarkDotNet.Attributes;
using Lails.CrudBuilder.CrudBuilder;
using Lails.CrudBuilder.DBContext;
using Lails.CrudBuilder.Extensions;
using Lails.CrudBuilder.Tests.BusinessLogic.Commands;
using Lails.CrudBuilder.Tests.BusinessLogic.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Transactions;

namespace Lails.CrudBuilder.Benchmarks.Benchmarks;

/// <summary>
/// Бенчмарки для варианта с разделением контекста на чтение и запись (CrudBuilder&lt;TReadDbContext, TWriteDbContext&gt;).
/// Сравнимы по сценариям с <see cref="CrudBuilderBenchmarks"/>.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(invocationCount: 100, warmupCount: 5, iterationCount: 10)]
public class CrudBuilderReadWriteBenchmarks
{
    private ICrudBuilder _crudBuilder = null!;
    private LailsDbContext _dbContext = null!;
    private IServiceProvider _serviceProvider = null!;
    private string _connectionString = null!;
    private string _databaseName = null!;

    /// <summary>
    /// Отдельный контекст для чтения в бенчмарке.
    /// </summary>
    private class BenchmarkReadDbContext : DbContext
    {
        public BenchmarkReadDbContext(DbContextOptions<BenchmarkReadDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().HasKey(r => r.Id);
            modelBuilder.Entity<Invoice>().HasKey(r => r.Id);
        }
    }

    /// <summary>
    /// Отдельный контекст для записи в бенчмарке.
    /// </summary>
    private class BenchmarkWriteDbContext : DbContext
    {
        public BenchmarkWriteDbContext(DbContextOptions<BenchmarkWriteDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>().HasKey(r => r.Id);
            modelBuilder.Entity<Invoice>().HasKey(r => r.Id);
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        _connectionString = Environment.GetEnvironmentVariable("BENCHMARK_DB_CONNECTION")
            ?? "User ID=postgres;Password=Qq25252525;Host=localhost;Port=5432;Database=CrudBuilderBenchmarksReadWrite;";

        // Извлекаем имя базы данных из connection string
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        _databaseName = builder.Database ?? "CrudBuilderBenchmarksReadWrite";

        // Создаем connection string для системной БД (postgres) для удаления тестовой БД
        builder.Database = "postgres";
        var adminConnectionString = builder.ConnectionString;

        var services = new ServiceCollection();

        // Контекст, использующийся для миграций и очистки (из проекта DBContext)
        services
            .AddDbContext<LailsDbContext>(options =>
                options.UseNpgsql(_connectionString));

        // Отдельные контексты для чтения и записи в бенчмарке
        services
            .AddDbContext<BenchmarkReadDbContext>(options =>
                options.UseNpgsql(_connectionString));

        services
            .AddDbContext<BenchmarkWriteDbContext>(options =>
                options.UseNpgsql(_connectionString));

        services
            .AddDbCrud<BenchmarkReadDbContext, BenchmarkWriteDbContext>()
            .RegisterQueriesAndCommands<Lails.CrudBuilder.Tests.BusinessLogic.Queries.CustomerQuery, Lails.CrudBuilder.Tests.BusinessLogic.Commands.CustomerCommands>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<LailsDbContext>();

        // Применяем миграции один раз перед прогоном бенчмарков
        _dbContext.Database.Migrate();

        _crudBuilder = _serviceProvider.GetRequiredService<ICrudBuilder>();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Очищаем данные между итерациями для стабильной производительности
        _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Invoices\" CASCADE;").GetAwaiter().GetResult();
        _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Customers\" CASCADE;").GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            _dbContext?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();

            // Удаляем тестовую базу данных
            if (!string.IsNullOrEmpty(_connectionString) && !string.IsNullOrEmpty(_databaseName))
            {
                var builder = new NpgsqlConnectionStringBuilder(_connectionString);
                builder.Database = "postgres"; // Подключаемся к системной БД
                var adminConnectionString = builder.ConnectionString;

                using var connection = new NpgsqlConnection(adminConnectionString);
                connection.Open();

                // Завершаем все активные соединения к тестовой БД
                using var terminateCmd = new NpgsqlCommand(
                    $@"SELECT pg_terminate_backend(pg_stat_activity.pid)
                       FROM pg_stat_activity
                       WHERE pg_stat_activity.datname = '{_databaseName}'
                         AND pid <> pg_backend_pid();",
                    connection);
                terminateCmd.ExecuteNonQuery();

                // Удаляем базу данных
                using var dropCmd = new NpgsqlCommand(
                    $@"DROP DATABASE IF EXISTS ""{_databaseName}"";",
                    connection);
                dropCmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку, но не прерываем выполнение
            Console.WriteLine($"Ошибка при удалении базы данных (read/write): {ex.Message}");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task CreateCustomer_ReadWrite_WithoutTransaction()
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
        await cmd.Create(customer);
    }

    [Benchmark]
    public async Task CreateCustomer_ReadWrite_WithTransaction()
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        });
    }

    [Benchmark]
    public async Task CreateCustomer_ReadWrite_WithTransaction_RetryCount2()
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        }, retryCount: 2);
    }

    [Benchmark]
    public async Task CreateCustomer_ReadWrite_WithTransaction_RetryCount3()
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        }, retryCount: 3);
    }

    [Benchmark]
    public async Task CreateCustomer_ReadWrite_WithTransaction_Serializable()
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        }, IsolationLevel.Serializable);
    }

    [Benchmark]
    public async Task CreateCustomer_ReadWrite_WithTransaction_WithDelay()
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        }, retryDelay: TimeSpan.FromMilliseconds(10));
    }

    [Benchmark]
    public async Task CreateAndUpdate_ReadWrite_WithTransaction()
    {
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
            customer.FirstName = "Updated";
            await cmd.Update(customer);
        });
    }

    [Benchmark]
    public async Task CreateMultipleCustomers_ReadWrite_WithTransaction()
    {
        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            for (int i = 0; i < 10; i++)
            {
                var customer = new Customer
                {
                    FirstName = $"Test{i}",
                    LastName = "User",
                    Address = "Test Address"
                };
                await cmd.Create(customer);
            }
        });
    }

    [Benchmark]
    public async Task ReadCustomers_ReadWrite_Query_ReadContext()
    {
        // Сначала создаем данные через write context для последующего чтения
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        });

        // Чтение через read context (BuildQuery использует read context)
        var query = _crudBuilder.BuildQuery<CustomerQuery>();
        var filter = CustomerFilter.Create()
            .SetId(customer.Id);
        await query.GetByFilterAsNoTracking(filter);
    }

    [Benchmark]
    public async Task Write_ReadWrite_Command_WriteContext()
    {
        // Запись через write context (BuildCommand использует write context)
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
        await cmd.Create(customer);
    }

    [Benchmark]
    public async Task WriteThenRead_ReadWrite_SeparateContexts()
    {
        // Запись через write context
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        });

        // Чтение через read context (в реальном сценарии read/write split данные могут быть не видны сразу)
        var query = _crudBuilder.BuildQuery<CustomerQuery>();
        var filter = CustomerFilter.Create()
            .SetId(customer.Id);
        await query.GetByFilterAsNoTracking(filter);
    }

    [Benchmark]
    public async Task MultipleReads_ReadWrite_ReadContextOnly()
    {
        // Создаем несколько записей через write context
        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            for (int i = 0; i < 5; i++)
            {
                var customer = new Customer
                {
                    FirstName = $"Test{i}",
                    LastName = "User",
                    Address = "Test Address"
                };
                await cmd.Create(customer);
            }
        });

        // Множественные чтения через read context (BuildQuery всегда использует read context)
        var query = _crudBuilder.BuildQuery<CustomerQuery>();
        var filter = CustomerFilter.Create();
        
        // Первое чтение
        await query.GetByFilterAsNoTracking(filter);
        
        // Второе чтение
        await query.GetByFilterAsNoTracking(filter);
        
        // Третье чтение
        await query.GetByFilterAsNoTracking(filter);
    }

    [Benchmark]
    public async Task MultipleWrites_ReadWrite_WriteContextOnly()
    {
        // Множественные записи через write context (BuildCommand всегда использует write context)
        var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
        
        for (int i = 0; i < 5; i++)
        {
            var customer = new Customer
            {
                FirstName = $"Test{i}",
                LastName = "User",
                Address = "Test Address"
            };
            await cmd.Create(customer);
        }
    }

    [Benchmark]
    public async Task ReadWriteMixed_ReadWrite_SeparateContexts()
    {
        // Комбинированный сценарий: запись через write context, затем чтение через read context
        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Address = "Test Address"
        };

        // Запись через write context
        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd.Create(customer);
        });

        // Чтение через read context
        var query = _crudBuilder.BuildQuery<CustomerQuery>();
        var filter = CustomerFilter.Create()
            .SetId(customer.Id);
        var result = await query.GetByFilterAsNoTracking(filter);

        // Еще одна запись через write context
        var customer2 = new Customer
        {
            FirstName = "Test2",
            LastName = "User2",
            Address = "Test Address2"
        };

        await _crudBuilder.WithTransaction(async () =>
        {
            var cmd2 = _crudBuilder.BuildCommand<CustomerCommands>();
            await cmd2.Create(customer2);
        });

        // Еще одно чтение через read context
        var filter2 = CustomerFilter.Create()
            .SetId(customer2.Id);
        await query.GetByFilterAsNoTracking(filter2);
    }
}



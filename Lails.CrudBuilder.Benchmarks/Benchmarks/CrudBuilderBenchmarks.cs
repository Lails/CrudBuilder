using BenchmarkDotNet.Attributes;
using Lails.CrudBuilder.CrudBuilder;
using Lails.CrudBuilder.DBContext;
using Lails.CrudBuilder.Extensions;
using Lails.CrudBuilder.Tests.BusinessLogic.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Transactions;

namespace Lails.CrudBuilder.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 50, warmupCount: 5, iterationCount: 10)]
public class CrudBuilderBenchmarks
{
    private ICrudBuilder _crudBuilder = null!;
    private LailsDbContext _dbContext = null!;
    private IServiceProvider _serviceProvider = null!;
    private string _connectionString = null!;
    private string _databaseName = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connectionString = Environment.GetEnvironmentVariable("BENCHMARK_DB_CONNECTION")
            ?? "User ID=postgres;Password=Qq25252525;Host=localhost;Port=5432;Database=CrudBuilderBenchmarks;";

        // Извлекаем имя базы данных из connection string
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        _databaseName = builder.Database ?? "CrudBuilderBenchmarks";

        // Создаем connection string для системной БД (postgres) для удаления тестовой БД
        builder.Database = "postgres";
        var adminConnectionString = builder.ConnectionString;

        var services = new ServiceCollection();

        services
            .AddDbContextPool<LailsDbContext>(options =>
                options.UseNpgsql(_connectionString));

        services
            .AddDbCrud<LailsDbContext>()
            .RegisterQueriesAndCommands<Lails.CrudBuilder.Tests.BusinessLogic.Queries.CustomerQuery, Lails.CrudBuilder.Tests.BusinessLogic.Commands.CustomerCommands>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<LailsDbContext>();

        // Применяем миграции
        _dbContext.Database.Migrate();

        _crudBuilder = _serviceProvider.GetRequiredService<ICrudBuilder>();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Очищаем данные между итерациями для стабильной производительности
        // Используем TRUNCATE для быстрой очистки таблиц (CASCADE удалит связанные данные)
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
            Console.WriteLine($"Ошибка при удалении базы данных: {ex.Message}");
        }
    }

    [Benchmark(Baseline = true)]
    public async Task CreateCustomer_WithoutTransaction()
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
    public async Task CreateCustomer_WithTransaction()
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
    public async Task CreateCustomer_WithTransaction_RetryCount2()
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
    public async Task CreateCustomer_WithTransaction_RetryCount3()
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
    public async Task CreateCustomer_WithTransaction_Serializable()
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
    public async Task CreateCustomer_WithTransaction_WithDelay()
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
    public async Task CreateAndUpdate_WithTransaction()
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
    public async Task CreateMultipleCustomers_WithTransaction()
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
}


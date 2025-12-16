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
[SimpleJob(invocationCount: 20, warmupCount: 3, iterationCount: 5)]
public class CrudBuilderStressTests
{
    private ICrudBuilder _crudBuilder = null!;
    private LailsDbContext _dbContext = null!;
    private IServiceProvider _serviceProvider = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private string _connectionString = null!;
    private string _databaseName = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connectionString = Environment.GetEnvironmentVariable("BENCHMARK_DB_CONNECTION") 
            ?? "User ID=postgres;Password=Qq25252525;Host=localhost;Port=5432;Database=CrudBuilderStressTests;";

        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        _databaseName = builder.Database ?? "CrudBuilderStressTests";
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
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        _dbContext = _serviceProvider.GetRequiredService<LailsDbContext>();
        
        _dbContext.Database.Migrate();
        
        _crudBuilder = _serviceProvider.GetRequiredService<ICrudBuilder>();
    }

    [IterationSetup]
    public void IterationSetup()
    {
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

            if (!string.IsNullOrEmpty(_connectionString) && !string.IsNullOrEmpty(_databaseName))
            {
                var builder = new NpgsqlConnectionStringBuilder(_connectionString);
                builder.Database = "postgres";
                var adminConnectionString = builder.ConnectionString;

                using var connection = new NpgsqlConnection(adminConnectionString);
                connection.Open();

                using var terminateCmd = new NpgsqlCommand(
                    $@"SELECT pg_terminate_backend(pg_stat_activity.pid)
                       FROM pg_stat_activity
                       WHERE pg_stat_activity.datname = '{_databaseName}'
                         AND pid <> pg_backend_pid();",
                    connection);
                terminateCmd.ExecuteNonQuery();

                using var dropCmd = new NpgsqlCommand(
                    $@"DROP DATABASE IF EXISTS ""{_databaseName}"";",
                    connection);
                dropCmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при удалении базы данных: {ex.Message}");
        }
    }

    /// <summary>
    /// Стресс-тест: симуляция DBConcurrencyException с retryCount=1 (должен выбросить исключение после первой попытки)
    /// </summary>
    [Benchmark]
    public async Task StressTest_RetryCount1_ThrowsAfterFirstAttempt()
    {
        int attemptCount = 0;
        try
        {
            await _crudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount++;
                throw new System.Data.DBConcurrencyException($"Concurrency conflict attempt {attemptCount}");
            }, IsolationLevel.ReadCommitted, retryCount: 1);
        }
        catch (System.Data.DBConcurrencyException)
        {
            // Ожидаемое исключение
        }
    }

    /// <summary>
    /// Стресс-тест: симуляция DBConcurrencyException с retryCount=2 (должен выбросить исключение после второй попытки)
    /// </summary>
    [Benchmark]
    public async Task StressTest_RetryCount2_ThrowsAfterSecondAttempt()
    {
        int attemptCount = 0;
        try
        {
            await _crudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount++;
                throw new System.Data.DBConcurrencyException($"Concurrency conflict attempt {attemptCount}");
            }, IsolationLevel.ReadCommitted, retryCount: 2);
        }
        catch (System.Data.DBConcurrencyException)
        {
            // Ожидаемое исключение
        }
    }

    /// <summary>
    /// Стресс-тест: симуляция DBConcurrencyException с retryCount=3 (должен выбросить исключение после третьей попытки)
    /// </summary>
    [Benchmark]
    public async Task StressTest_RetryCount3_ThrowsAfterThirdAttempt()
    {
        int attemptCount = 0;
        try
        {
            await _crudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount++;
                throw new System.Data.DBConcurrencyException($"Concurrency conflict attempt {attemptCount}");
            }, IsolationLevel.ReadCommitted, retryCount: 3);
        }
        catch (System.Data.DBConcurrencyException)
        {
            // Ожидаемое исключение
        }
    }

    /// <summary>
    /// Стресс-тест: успешный retry после первой неудачи (retryCount=2, первая попытка падает, вторая успешна)
    /// </summary>
    [Benchmark]
    public async Task StressTest_RetryCount2_SecondAttemptSucceeds()
    {
        int attemptCount = 0;
        await _crudBuilder.WithTransaction(async () =>
        {
            await Task.CompletedTask;
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new System.Data.DBConcurrencyException("Concurrency conflict - first attempt");
            }
            // Вторая попытка успешна
        }, IsolationLevel.ReadCommitted, retryCount: 2);
    }

    /// <summary>
    /// Стресс-тест: успешный retry после двух неудач (retryCount=3, первые две попытки падают, третья успешна)
    /// </summary>
    [Benchmark]
    public async Task StressTest_RetryCount3_ThirdAttemptSucceeds()
    {
        int attemptCount = 0;
        await _crudBuilder.WithTransaction(async () =>
        {
            await Task.CompletedTask;
            attemptCount++;
            if (attemptCount <= 2)
            {
                throw new System.Data.DBConcurrencyException($"Concurrency conflict - attempt {attemptCount}");
            }
            // Третья попытка успешна
        }, IsolationLevel.ReadCommitted, retryCount: 3);
    }

    /// <summary>
    /// Стресс-тест: retry с задержкой (retryDelay=50ms, retryCount=2)
    /// </summary>
    [Benchmark]
    public async Task StressTest_RetryWithDelay_50ms()
    {
        int attemptCount = 0;
        try
        {
            await _crudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount++;
                throw new System.Data.DBConcurrencyException($"Concurrency conflict attempt {attemptCount}");
            }, IsolationLevel.ReadCommitted, retryCount: 2, retryDelay: TimeSpan.FromMilliseconds(50));
        }
        catch (System.Data.DBConcurrencyException)
        {
            // Ожидаемое исключение
        }
    }

    /// <summary>
    /// Стресс-тест: реальный конфликт параллелизма - параллельное обновление одной записи
    /// </summary>
    [Benchmark]
    public async Task StressTest_RealConcurrencyConflict_ParallelUpdates()
    {
        // Создаем клиента
        var customer = new Customer
        {
            FirstName = "Stress",
            LastName = "Test",
            Address = "Test Address"
        };

        await _crudBuilder.BuildCommand<CustomerCommands>().Create(customer);

        // Параллельные обновления одной записи для создания реального конфликта
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _crudBuilder.WithTransaction(async () =>
                    {
                        // Создаем новый контекст для каждого потока
                        using var scope = _scopeFactory.CreateScope();
                        var ctx = scope.ServiceProvider.GetRequiredService<LailsDbContext>();
                        var cust = await ctx.Customers.FindAsync(customer.Id);
                        if (cust != null)
                        {
                            cust.FirstName = $"Updated_{index}";
                            await ctx.SaveChangesAsync();
                        }
                    }, IsolationLevel.ReadCommitted, retryCount: 3);
                }
                catch
                {
                    // Игнорируем ошибки - это ожидаемо при конфликтах
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Стресс-тест: множественные операции с retry (10 операций, каждая может вызвать конфликт)
    /// </summary>
    [Benchmark]
    public async Task StressTest_MultipleOperations_WithRetry()
    {
        int successCount = 0;
        int failureCount = 0;

        for (int i = 0; i < 10; i++)
        {
            int attemptCount = 0;
            try
            {
                await _crudBuilder.WithTransaction(async () =>
                {
                    await Task.CompletedTask;
                    attemptCount++;
                    var customer = new Customer
                    {
                        FirstName = $"Stress_{i}",
                        LastName = "Test",
                        Address = "Test Address"
                    };
                    await _crudBuilder.BuildCommand<CustomerCommands>().Create(customer);
                    
                    // Симулируем конфликт в 30% случаев
                    if (i % 3 == 0 && attemptCount == 1)
                    {
                        throw new System.Data.DBConcurrencyException("Simulated conflict");
                    }
                }, IsolationLevel.ReadCommitted, retryCount: 2);
                successCount++;
            }
            catch (System.Data.DBConcurrencyException)
            {
                failureCount++;
            }
        }
    }

    /// <summary>
    /// Стресс-тест: Serializable изоляция с конфликтами
    /// </summary>
    [Benchmark]
    public async Task StressTest_SerializableIsolation_WithConflicts()
    {
        int attemptCount = 0;
        try
        {
            await _crudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount++;
                throw new System.Data.DBConcurrencyException($"Serializable conflict attempt {attemptCount}");
            }, IsolationLevel.Serializable, retryCount: 3);
        }
        catch (System.Data.DBConcurrencyException)
        {
            // Ожидаемое исключение
        }
    }
}


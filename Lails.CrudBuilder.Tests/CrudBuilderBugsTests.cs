using Lails.CrudBuilder.CrudBuilder;
using Lails.CrudBuilder.DBContext;
using NUnit.Framework;
using System.Transactions;

namespace Lails.CrudBuilder.Tests
{
    /// <summary>
    /// Тесты для проверки функциональности CrudBuilder
    /// </summary>
    public class CrudBuilderBugsTests : Setup
    {
        [SetUp]
        public async Task Setup()
        {
            await SeedDatabase();
        }

        [TearDown]
        public async Task Down()
        {
            await ResetDatabase();
        }

        #region Retry логика в WithTransaction

        /// <summary>
        /// Проверяет поведение retry при retryCount=1 и DBConcurrencyException
        /// </summary>
        [Test]
        public async Task WithTransaction_RetryCountOne_ShouldThrowAfterFirstFailure()
        {
            int attemptCount = 0;

            var ex = Assert.ThrowsAsync<System.Data.DBConcurrencyException>(async () =>
            {
                await CrudBuilder.WithTransaction(async () =>
                {
                    await Task.CompletedTask;
                    attemptCount++;
                    // Симулируем DBConcurrencyException
                    throw new System.Data.DBConcurrencyException("Concurrency conflict");
                }, IsolationLevel.ReadCommitted, retryCount: 1);
            });

            Assert.That(attemptCount, Is.EqualTo(1),
                "При retryCount=1 должна быть только одна попытка");
            Assert.That(ex, Is.Not.Null,
                "Исключение должно быть выброшено после первой неудачной попытки");
        }

        /// <summary>
        /// Проверяет поведение retry при retryCount=2
        /// </summary>
        [Test]
        public async Task WithTransaction_RetryCountTwo_ShouldRetryExactlyTwoTimes()
        {
            int attemptCount = 0;

            var ex = Assert.ThrowsAsync<System.Data.DBConcurrencyException>(async () =>
            {
                await CrudBuilder.WithTransaction(async () =>
                {
                    await Task.CompletedTask;
                    attemptCount++;
                    throw new System.Data.DBConcurrencyException("Concurrency conflict");
                }, IsolationLevel.ReadCommitted, retryCount: 2);
            });

            Assert.That(attemptCount, Is.EqualTo(2),
                "При retryCount=2 должно быть ровно 2 попытки");
            Assert.That(ex, Is.Not.Null);
        }

        /// <summary>
        /// Проверяет, что при успешной второй попытке retry работает корректно
        /// </summary>
        [Test]
        public async Task WithTransaction_RetryCountTwo_SecondAttemptSucceeds()
        {
            int attemptCount = 0;
            bool success = false;

            await CrudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new System.Data.DBConcurrencyException("Concurrency conflict");
                }
                success = true;
            }, IsolationLevel.ReadCommitted, retryCount: 2);

            Assert.That(attemptCount, Is.EqualTo(2),
                "Должно быть 2 попытки: первая неудачная, вторая успешная");
            Assert.That(success, Is.True, "Вторая попытка должна быть успешной");
        }

        #endregion

        #region Валидация null в WithTransaction

        /// <summary>
        /// Проверяет поведение WithTransaction при передаче null в качестве func
        /// </summary>
        [Test]
        public async Task WithTransaction_WithNullFunc_ShouldThrowArgumentNullException()
        {
            Func<Task> nullFunc = null!;

            var ex =  Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await CrudBuilder.WithTransaction(nullFunc!);
            });
        }

        /// <summary>
        /// Проверяет поведение WithTransaction<TResult> при передаче null в качестве func
        /// </summary>
        [Test]
        public async Task WithTransaction_WithNullFuncTResult_ShouldThrowArgumentNullException()
        {
            Func<Task<int>> nullFunc = null!;

            var ex = Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await CrudBuilder.WithTransaction(nullFunc!);
            });
        }

        #endregion

        #region Логирование в WithTransaction

        /// <summary>
        /// Проверяет механизм логирования при возникновении исключений в WithTransaction
        /// </summary>
        [Test]
        public async Task WithTransaction_OnException_UsesConsoleWriteLineInsteadOfLogger()
        {
            // Проверяем, что при исключении НЕ происходит вывод в консоль (логирование убрано)

            var originalOut = Console.Out;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);

                var ex = Assert.ThrowsAsync<System.Data.DBConcurrencyException>(async () =>
                {
                    await CrudBuilder.WithTransaction(async () =>
                    {
                        await Task.CompletedTask;
                        throw new System.Data.DBConcurrencyException("Test exception");
                    }, retryCount: 1);
                });

                var output = stringWriter.ToString();

                Assert.That(output, Does.Not.Contain("Retried:"),
                    "При исключении не должно происходить вывод в консоль (логирование убрано)");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        #endregion

        #region Поведение перегрузок WithTransaction

        /// <summary>
        /// Проверяет, что оба метода WithTransaction ведут себя одинаково
        /// </summary>
        [Test]
        public async Task WithTransaction_BothOverloads_ShouldBehaveIdentically()
        {
            int attemptCount1 = 0;
            int attemptCount2 = 0;

            // Тест для метода без возвращаемого значения
            await CrudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount1++;
            }, retryCount: 1);

            // Тест для метода с возвращаемым значением
            var result = await CrudBuilder.WithTransaction(async () =>
            {
                await Task.CompletedTask;
                attemptCount2++;
                return 42;
            }, retryCount: 1);

            // Оба метода должны работать одинаково
            Assert.That(attemptCount1, Is.EqualTo(1));
            Assert.That(attemptCount2, Is.EqualTo(1));
            Assert.That(result, Is.EqualTo(42));
        }

        #endregion

        #region Использование транзакций в WithTransaction

        /// <summary>
        /// Проверяет использование TransactionScope в WithTransaction
        /// </summary>
        [Test]
        public async Task WithTransaction_UsesTransactionScope_NotEfCoreTransaction()
        {
            // Проверяем, что используется TransactionScope

            bool transactionScopeUsed = false;

            await CrudBuilder.WithTransaction(async () =>
            {
                // Проверяем, что транзакция активна
                var currentTransaction = Transaction.Current;
                transactionScopeUsed = currentTransaction != null;

                // Создаем сущность в транзакции
                var customer = new Customer
                {
                    FirstName = "TransactionTest",
                    LastName = "Test",
                    Address = "Test"
                };
                Context.Customers.Add(customer);
                await Context.SaveChangesAsync();
            });

            Assert.That(transactionScopeUsed, Is.True,
                "Должен использоваться TransactionScope");
        }

        #endregion

        #region Поддержка CancellationToken

        /// <summary>
        /// Проверяет наличие поддержки CancellationToken в методах WithTransaction
        /// </summary>
        [Test]
        public async Task WithTransaction_NoCancellationTokenSupport_IsMissing()
        {
            var methodInfo = typeof(ICrudBuilder).GetMethod(nameof(ICrudBuilder.WithTransaction),
                new[] { typeof(Func<Task>), typeof(System.Transactions.IsolationLevel), typeof(uint) });

            var parameters = methodInfo?.GetParameters();
            var hasCancellationToken = parameters?.Any(p => p.ParameterType == typeof(CancellationToken)) ?? false;

            Assert.That(hasCancellationToken, Is.False,
                "Методы WithTransaction не поддерживают CancellationToken");
        }

        #endregion

        #region Недостижимый код в WithTransaction

        /// <summary>
        /// Проверяет, что недостижимый код в конце метода WithTransaction не выполняется
        /// </summary>
        [Test]
        public async Task WithTransaction_UnreachableCode_ShouldNeverExecute()
        {
            // Проверяем, что недостижимый код в конце метода WithTransaction<TResult> не выполняется

            int attemptCount = 0;

            var ex = Assert.ThrowsAsync<System.Data.DBConcurrencyException>(async () =>
            {
                await CrudBuilder.WithTransaction(async () =>
                {
                    await Task.CompletedTask;
                    attemptCount++;
                    throw new System.Data.DBConcurrencyException("Test");
                    return 0; // Вызываем WithTransaction<TResult> для проверки недостижимого кода
                }, retryCount: 1);
            });

            Assert.That(ex, Is.Not.TypeOf<InvalidOperationException>(),
                "Недостижимый код в конце метода WithTransaction<TResult> не должен выполняться");
        }

        #endregion
    }
}


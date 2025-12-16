using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace Lails.CrudBuilder.CrudBuilder
{
    /// <summary>
    /// Основной контракт для построения CQRS-команд и запросов,
    /// а также выполнения операций в транзакции с поддержкой повторных попыток.
    /// </summary>
    public interface ICrudBuilder
    {
        /// <summary>
        /// Создает экземпляр запроса <typeparamref name="TQuery"/> и привязывает к нему текущий <see cref="DbContext"/>.
        /// </summary>
        /// <typeparam name="TQuery">Тип запроса, наследник <see cref="BaseQuery"/>.</typeparam>
        /// <returns>Сконфигурированный экземпляр запроса.</returns>
        TQuery BuildQuery<TQuery>()
            where TQuery : BaseQuery;

        /// <summary>
        /// Создает экземпляр команды <typeparamref name="TCommand"/> и привязывает к нему текущий <see cref="DbContext"/>.
        /// </summary>
        /// <typeparam name="TCommand">Тип команды, наследник <see cref="BaseCommand"/>.</typeparam>
        /// <returns>Сконфигурированный экземпляр команды.</returns>
        TCommand BuildCommand<TCommand>()
            where TCommand : BaseCommand;

        /// <summary>
        /// Выполняет асинхронную операцию в транзакции с поддержкой повторных попыток при конфликтах параллелизма.
        /// </summary>
        /// <typeparam name="TResult">Тип возвращаемого результата.</typeparam>
        /// <param name="func">Функция, содержащая бизнес-логику, выполняемую в транзакции.</param>
        /// <param name="isolationLevel">Уровень изоляции транзакции.</param>
        /// <param name="retryCount">Количество попыток при возникновении <see cref="System.Data.DBConcurrencyException"/>.</param>
        /// <param name="retryDelay">
        /// Базовая задержка между повторными попытками. При использовании backoff фактическая задержка увеличивается.
        /// </param>
        /// <param name="cancellationToken">Токен отмены, позволяющий прервать операцию.</param>
        /// <returns>Результат, возвращаемый функцией <paramref name="func"/>.</returns>
        Task<TResult> WithTransaction<TResult>(
            Func<Task<TResult>> func,
            IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
            uint retryCount = 1,
            TimeSpan? retryDelay = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Выполняет асинхронную операцию в транзакции с поддержкой повторных попыток при конфликтах параллелизма.
        /// Подходит для операций без возвращаемого значения.
        /// </summary>
        /// <param name="func">Функция, содержащая бизнес-логику, выполняемую в транзакции.</param>
        /// <param name="isolationLevel">Уровень изоляции транзакции.</param>
        /// <param name="retryCount">Количество попыток при возникновении <see cref="System.Data.DBConcurrencyException"/>.</param>
        /// <param name="retryDelay">
        /// Базовая задержка между повторными попытками. При использовании backoff фактическая задержка увеличивается.
        /// </param>
        /// <param name="cancellationToken">Токен отмены, позволяющий прервать операцию.</param>
        Task WithTransaction(Func<Task> func,
            IsolationLevel isolationLevel = IsolationLevel.RepeatableRead,
            uint retryCount = 1,
            TimeSpan? retryDelay = null,
            CancellationToken cancellationToken = default);
    }
}

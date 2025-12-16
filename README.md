## Lails.CrudBuilder

Лёгкая CQRS/CRUD-надстройка над Entity Framework Core с транзакциями и retry-логикой.

### Установка и регистрация

```csharp
// Startup / Program
services.AddDbContextPool<LailsDbContext>(options =>
    options.UseNpgsql(connectionString));

services
    .AddDbCrud<LailsDbContext>()
    .RegisterQueriesAndCommands<MyQueryAssemblyMarker, MyCommandAssemblyMarker>();
```

`MyQueryAssemblyMarker` и `MyCommandAssemblyMarker` — любые типы из сборок, где лежат ваши `BaseQuery` и `BaseCommand`.

### Команды (Create/Update/Delete)

```csharp
public class CustomerCommands : BaseCommand
{
    public async Task<Guid> Create(Customer customer)
    {
        var set = GetSet<Customer>();
        await set.AddAsync(customer);
        await SaveChangesAsync();
        return customer.Id;
    }
}

// Использование
var cmd = _crudBuilder.BuildCommand<CustomerCommands>();
var id = await cmd.Create(customer);
или
var id = await _crudBuilder.BuildCommand<CustomerCommands>().Create(customer);
```

### Запросы (Read)

```csharp
public class CustomerQuery : BaseQuery
{
    // Простейший запрос - получить всех клиентов
    public IQueryable<Customer> GetAll()
        => GetAsNoTracking<Customer>();

    // Более реальный пример: несколько разных запросов в одном query-классе
    public IQueryable<Customer> GetByCity(string city)
        => GetAsNoTracking<Customer>()
            .Where(c => c.Address == city);

    public IQueryable<Customer> Search(string? namePart, string? city)
    {
        var query = GetAsNoTracking<Customer>();

        if (!string.IsNullOrWhiteSpace(namePart))
        {
            query = query.Where(c =>
                c.FirstName.Contains(namePart) ||
                c.LastName.Contains(namePart));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(c => c.Address == city);
        }

        return query;
    }
}

// Использование
var query = _crudBuilder.BuildQuery<CustomerQuery>();

// 1. Все клиенты
var all = await query.GetAll().ToListAsync();

// 2. Клиенты по городу
var inSydney = await query.GetByCity("Sydney").ToListAsync();

// 3. Клиенты по фильтру
var filtered = await query.Search("Angry", "Sydney").ToListAsync();
```

### Транзакции с retry

```csharp
// Реальный кейс: несколько операций в одной транзакции
await _crudBuilder.WithTransaction(async () =>
{
    var cmd = _crudBuilder.BuildCommand<CustomerCommands>();

    // 1. Создаём клиента
    var customerId = await cmd.Create(customer);

    // 2. Обновляем этого же клиента
    customer.FirstName = "Updated";
    await cmd.Update(customer);

    // 3. Создаём несколько связанных сущностей (пример)
    foreach (var invoice in invoices)
    {
        invoice.CustomerId = customerId;
        await cmd.CreateInvoice(invoice);
    }
},
isolationLevel: IsolationLevel.ReadCommitted,
retryCount: 2,
retryDelay: TimeSpan.FromMilliseconds(100),
cancellationToken: cancellationToken);
```

### Потоковое чтение больших выборок

```csharp
public class CustomerStreamQuery : BaseQuery
{
    public async IAsyncEnumerable<Customer> GetAllStream()
    {
        await foreach (var c in GetAsNoTrackingStream<Customer>())
            yield return c;
    }
}
``` 

### Полный список доступных API

#### Расширения регистрации (`Lails.CrudBuilder.Extensions`)

- `IServiceCollection AddDbCrud<TDbContext>()`
  - Регистрирует `ICrudBuilder` c указанным `DbContext`.
  - Возвращает `IRegisterQueriesAndCommandExtension` для дальнейшей регистрации.

- `IServiceCollection RegisterQueriesAndCommands<TQueryAssemblyPointer, TCommandAssemblyPointer>()`
  - Сканирует сборку `TQueryAssemblyPointer` и регистрирует все типы, наследующие `BaseQuery`.
  - Сканирует сборку `TCommandAssemblyPointer` и регистрирует все типы, наследующие `BaseCommand`.

#### Интерфейс `ICrudBuilder`

- `TQuery BuildQuery<TQuery>() where TQuery : BaseQuery`
  - Создаёт и настраивает экземпляр query-класса с текущим `DbContext`.

- `TCommand BuildCommand<TCommand>() where TCommand : BaseCommand`
  - Создаёт и настраивает экземпляр command-класса с текущим `DbContext`.

- `Task<TResult> WithTransaction<TResult>(Func<Task<TResult>> func, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead, uint retryCount = 1, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)`
  - Выполняет функцию `func` в транзакции с retry по `DBConcurrencyException`.

- `Task WithTransaction(Func<Task> func, IsolationLevel isolationLevel = IsolationLevel.RepeatableRead, uint retryCount = 1, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)`
  - То же самое, но для операций без возвращаемого значения.

#### Базовый класс `BaseCommand`

- `protected DbSet<TEntity> GetSet<TEntity>() where TEntity : class`
  - Доступ к `DbSet<TEntity>` текущего `DbContext`.

- `Task<int> SaveChangesAsync()`
- `Task<int> SaveChangesAsync(CancellationToken cancellationToken)`
- `int SaveChanges()`
- `int SaveChanges(bool acceptAllChangesOnSuccess)`
  - Стандартные методы сохранения изменений в базе.

#### Базовый класс `BaseQuery`

- `protected IQueryable<TEntity> GetAsNoTracking<TEntity>() where TEntity : class`
  - Запрос без трекинга, для чтения.

- `protected IQueryable<TEntity> GetAsTracking<TEntity>() where TEntity : class`
  - Запрос с трекингом, для модификации сущностей.

- `protected IAsyncEnumerable<TEntity> GetAsNoTrackingStream<TEntity>() where TEntity : class`
  - Потоковое чтение без трекинга (большие выборки).

- `protected IAsyncEnumerable<TEntity> GetAsTrackingStream<TEntity>() where TEntity : class`
  - Потоковое чтение с трекингом. Подходит для небольших выборок, где нужно модифицировать сущности и затем сохранить их через `SaveChanges`, так как все элементы попадают в `ChangeTracker` контекста. Для очень больших выборок лучше использовать вариант без трекинга.

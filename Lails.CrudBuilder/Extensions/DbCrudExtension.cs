using Lails.CrudBuilder.CrudBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lails.CrudBuilder.Extensions
{
    /// <summary>
    /// Расширения для регистрации <see cref="ICrudBuilder"/> и связанных CQRS-типов в DI-контейнере.
    /// </summary>
    public static class DbCrudExtension
    {
        /// <summary>
        /// Регистрирует <see cref="ICrudBuilder"/> и возвращает объект для дальнейшей регистрации команд и запросов.
        /// </summary>
        /// <typeparam name="TDbContext">Тип контекста базы данных.</typeparam>
        /// <param name="services">Коллекция сервисов DI-контейнера.</param>
        /// <returns>Интерфейс для регистрации CQRS-команд и запросов.</returns>
        public static IRegisterQueriesAndCommandExtension AddDbCrud<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services
                .AddTransient<ICrudBuilder, CrudBuilder<TDbContext>>();

            return new RegisterQueriesExtension(services);
        }
    }

    /// <summary>
    /// Контракт для регистрации CQRS-запросов и команд в DI-контейнере.
    /// </summary>
    public interface IRegisterQueriesAndCommandExtension
    {
        /// <summary>
        /// Регистрирует все типы, наследующие <see cref="BaseQuery"/> и <see cref="BaseCommand"/>,
        /// из указанных сборок в DI-контейнере.
        /// </summary>
        /// <typeparam name="TQueryAssemblyPointer">
        /// Тип-маркер из сборки, где расположены реализации запросов (<see cref="BaseQuery"/>).
        /// </typeparam>
        /// <typeparam name="TCommandAssemblyPointer">
        /// Тип-маркер из сборки, где расположены реализации команд (<see cref="BaseCommand"/>).
        /// </typeparam>
        /// <returns>Ту же коллекцию сервисов для дальнейшей конфигурации.</returns>
        IServiceCollection RegisterQueriesAndCommands<TQueryAssemblyPointer, TCommandAssemblyPointer>()
            where TQueryAssemblyPointer : class
            where TCommandAssemblyPointer : class;
    }

    /// <summary>
    /// Реализация <see cref="IRegisterQueriesAndCommandExtension"/>, использующая сканирование сборок
    /// для автоматической регистрации команд и запросов.
    /// </summary>
    public class RegisterQueriesExtension : IRegisterQueriesAndCommandExtension
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// Создает новый экземпляр <see cref="RegisterQueriesExtension"/>.
        /// </summary>
        public RegisterQueriesExtension(IServiceCollection services)
        {
            _services = services;
        }

        /// <inheritdoc />
        public IServiceCollection RegisterQueriesAndCommands<TQueryAssemblyPointer, TCommandAssemblyPointer>()
            where TQueryAssemblyPointer : class
            where TCommandAssemblyPointer : class
        {
            _services.Scan(scan => scan
                .FromAssemblyOf<TQueryAssemblyPointer>()
                .AddClasses(classes => classes.AssignableTo<BaseQuery>())
                .AsSelf()
                .WithTransientLifetime());

            _services.Scan(scan => scan
                .FromAssemblyOf<TCommandAssemblyPointer>()
                .AddClasses(classes => classes.AssignableTo<BaseCommand>())
                .AsSelf()
                .WithTransientLifetime());

            return _services;
        }
    }
}

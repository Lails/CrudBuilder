using Lails.CrudBuilder.CrudBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lails.CrudBuilder.Extansions
{
    public static class DbCrudExtansion
    {
        public static IRegisterQueriesAndCommandExtansion AddDbCrud<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services
                .AddTransient<ICrudBuilder, CrudBuilder<TDbContext>>();

            return new RegisterQueriesExtansion(services);
        }
    }

    public interface IRegisterQueriesAndCommandExtansion
    {
        IServiceCollection RegisterQueriesAndCommands<TQueryAssemplyPointer, TCommandAssemplyPointer>()
            where TQueryAssemplyPointer : class
            where TCommandAssemplyPointer : class;
    }

    public class RegisterQueriesExtansion : IRegisterQueriesAndCommandExtansion
    {
        private readonly IServiceCollection _services;
        public RegisterQueriesExtansion(IServiceCollection services)
        {
            _services = services;
        }
        public IServiceCollection RegisterQueriesAndCommands<TQueryAssemplyPointer, TCommandAssemplyPointer>()
            where TQueryAssemplyPointer : class
            where TCommandAssemplyPointer : class
        {
            _services.Scan(scan => scan
                .FromAssemblyOf<TQueryAssemplyPointer>()
                .AddClasses(classes => classes.AssignableTo<BaseQuery>())
                .AsSelf()
                .WithTransientLifetime());

            _services.Scan(scan => scan
                .FromAssemblyOf<TCommandAssemplyPointer>()
                .AddClasses(classes => classes.AssignableTo<BaseCommand>())
                .AsSelf()
                .WithTransientLifetime());

            return _services;
        }
    }
}

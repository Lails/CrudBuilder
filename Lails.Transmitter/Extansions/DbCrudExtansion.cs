using Lails.Transmitter.CrudBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lails.Transmitter.Extansions
{
    public static class DbCrudExtansion
    {
        public static IRegisterQueriesAndCommandExtansion AddDbCrud<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services
                //.AddTransient<IDbCrud<TDbContext>, DbCRUD<TDbContext>>()
                .AddTransient<CrudBuilder<TDbContext>, CrudBuilderImpl<TDbContext>>();

            return new RegisterQueriesExtansion(services);
        }
    }

    public interface IRegisterQueriesAndCommandExtansion
    {
        IServiceCollection RegisterQueriesAndCommands<TQueryAssemplyPointer, TQommandAssemplyPointer>()
            where TQueryAssemplyPointer : class
            where TQommandAssemplyPointer : class;
    }

    public class RegisterQueriesExtansion : IRegisterQueriesAndCommandExtansion
    {
        private readonly IServiceCollection _services;
        public RegisterQueriesExtansion(IServiceCollection services)
        {
            _services = services;
        }
        public IServiceCollection RegisterQueriesAndCommands<TQueryAssemplyPointer, TQommandAssemplyPointer>()
            where TQueryAssemplyPointer : class
            where TQommandAssemplyPointer : class
        {
            _services.Scan(scan => scan
                .FromAssemblyOf<TQueryAssemplyPointer>()
                .AddClasses(classes => classes.AssignableTo<BaseQuery>())
                .AsSelf()
                .WithTransientLifetime());

            _services.Scan(scan => scan
                .FromAssemblyOf<TQommandAssemplyPointer>()
                .AddClasses(classes => classes.AssignableTo<BaseQuery>())
                .AsSelf()
                .WithTransientLifetime());

            return _services;
        }
    }
}

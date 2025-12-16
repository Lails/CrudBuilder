using Lails.CrudBuilder.DBContext;
using Lails.CrudBuilder.Extensions;
using Lails.CrudBuilder.Load.Tetst.Consumers;
using Lails.CrudBuilder.Tests;
using Lails.MQ.Rabbit;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace Lails.CrudBuilder.Load.Tetst
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<LailsDbContext>(opt => opt.UseNpgsql(Configuration.GetConnectionString("TransmitterDbTests")), 100)
                .AddTransient<DbContext, LailsDbContext>();

            services
                .AddDbCrud<LailsDbContext>()
                .RegisterQueriesAndCommands<Setup, Setup>();


            services.AddMvc(r => { r.EnableEndpointRouting = false; });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Test service", Version = "v1" });
            });

            services.RegisterRabbitPublisher()
                .AddMassTransit(x =>
                {
                    x.AddConsumer<LoadTestConsumer>();

                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.AddDataBusConfiguration(Configuration);

                        cfg
                            .RegisterConsumerWithRetry<LoadTestConsumer, ILoadTestEvent>(context, 1, 1, 10);
                    });
                });
        }

        public void Configure(IApplicationBuilder app, IServiceProvider provider)
        {
            provider.GetRequiredService<LailsDbContext>().Database.Migrate();


            app.UseSwagger(c =>
            {
                c.RouteTemplate = "swagger/{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = $"https://{httpReq.Host.Value}" } };
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/v1/swagger.json", "Test service");
            });

            app.UseDeveloperExceptionPage();


            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}

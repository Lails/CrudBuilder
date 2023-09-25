using Lails.DBContext;
using Lails.Transmitter.Extansions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Lails.Template.Load.Tetst
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
            //services
            //	.AddEntityFrameworkInMemoryDatabase()
            //	.AddDbContext<LailsDbContext>((serviceProvider, options) => options.UseInMemoryDatabase("LailsDbContext").UseInternalServiceProvider(serviceProvider));

            services.AddDbContextPool<LailsDbContext>(opt => opt.UseNpgsql(Configuration.GetConnectionString("TransmitterDbTests")), 10)
                .AddTransient<LailsDbContext>();

            services
                .AddDbCrud<LailsDbContext>();

            services.AddMvc(r => { r.EnableEndpointRouting = false; }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);


            //services.AddMassTransit(x =>
            //{
            //	x.AddConsumer<LoadTestConsumer>();

            //	x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
            //	{
            //		cfg
            //			.AddDataBusConfiguration(services, Configuration);
            //		cfg
            //			.RegisterConsumerWithRetry<LoadTestConsumer, ILoadTestEvent>(provider, 1, 1);
            //	}));
            //});
            //services.RegisterDataBusPublisher();
        }

        public void Configure(IApplicationBuilder app,/* IBusControl busControl,*/ IServiceProvider provider)
        {
            provider.GetService<LailsDbContext>().Database.Migrate();

            //busControl.Start();

            app.UseDeveloperExceptionPage();


            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}

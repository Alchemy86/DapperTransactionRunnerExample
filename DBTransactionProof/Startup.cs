using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;

namespace DBTransactionProof
{
    internal class Startup
    {
        public void ConfigureHostConfiguration(IConfigurationBuilder cb)
        {
            // https://github.com/aspnet/Hosting/issues/1440 - Override hosting env for generic host
            cb.AddEnvironmentVariables("ASPNETCORE_");
            cb.SetBasePath(Directory.GetCurrentDirectory());
        }

        public void ConfigureLogging(HostBuilderContext hostBuilder, ILoggingBuilder lb)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(@"serilog.json", true);

            if (hostBuilder.HostingEnvironment.IsDevelopment())
                configuration.AddUserSecrets<Startup>();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration.Build())
                .CreateLogger();

            lb.AddSerilog();
        }

        public void ConfigureServices(HostBuilderContext hostBuilder, IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<IDummyRepo, DummyRepo1>();
            services.AddSingleton<IDummyRepo, DummyRepo2>();
            services.AddSingleton<ITransactionDelegate, TransactionDelegate>();
            services.AddSingleton<IHostedService, TransactionRunner>();
        }
    }
}

using System;
using Andy.EmailClient;
using Andy.SmsClient;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Andy.Functions.Startup))]
namespace Andy.Functions
{
    public class Startup : FunctionsStartup
    {

        public IConfiguration Configuration { get; }

        public Startup()
        {
        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var sp = ConfigureServices(builder.Services)
                .BuildServiceProvider(true);
            var eco = sp.GetService<IOptions<ExecutionContextOptions>>().Value;
            System.Environment.SetEnvironmentVariable("WebRootPath", eco.AppDirectory);
        }

        private IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services
                .AddApplicationInsightsTelemetry(
                    new ApplicationInsightsServiceOptions()
                    {
                        // https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core#using-applicationinsightsserviceoptions
                        // EnableAdaptiveSampling = false,
                        // EnableQuickPulseMetricStream = false
                    }
                    // TODO: Use TelemetryInitializers if we want to add global properties
                    // to all telemetry that is sent (maybe to identify a tenant).
                    // https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core#adding-telemetryinitializers
                )

                .AddSingleton<ISmsClient>(provider =>
                    new TwilioSmsClient(
                        System.Environment.GetEnvironmentVariable("TwilioAccountSid"),
                        System.Environment.GetEnvironmentVariable("TwilioAuthToken"),
                        System.Environment.GetEnvironmentVariable("TwilioFromNumber")
                    )
                )

                .AddSingleton<IEmailClient>(provider =>
                    new SendGridEmailClient(
                        System.Environment.GetEnvironmentVariable("SendgridApiKey"),
                        System.Environment.GetEnvironmentVariable("EmailFromAddress")
                    )
                )
                ;
            return services;
        }


    }
}
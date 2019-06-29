using chatProcessor.textAnalytics;
using cosmosDbConnection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: FunctionsStartup(typeof(chatProcessor.Startup))]
namespace chatProcessor
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<TextProcessor>();
            builder.Services.AddSingleton<StorageProcessor>();
        }
    }
}

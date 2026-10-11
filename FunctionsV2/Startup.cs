using Azure.Core;
using Azure.Identity;
using FunctionsV2.Common;
using FunctionsV2.Helpers;
using FunctionsV2.Sys_CampusJobs.Services.Interfaces;
using FunctionsV2.Sys_Cop_Agresso;
using FunctionsV2.Sys_HRMS.Services.Interfaces;
using FunctionsV2.Sys_StudentApp.Options;
using FunctionsV2.Sys_StudentApp.Services;
using FunctionsV2.Sys_StudentPhoto.Services;
using FunctionsV2.Sys_SymPay;
using FunctionV2.Sys_SymLib;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(FunctionsV2.Startup))]

namespace FunctionsV2
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ICache, Cache>();
            builder.Services.AddSingleton<IKeyVault, KeyVault>();
            builder.Services.AddSingleton<ISecrets, Secrets>();
            builder.Services.AddSingleton<IJwsService, JwsService>();
            builder.Services.AddSingleton<IOAuthToken, OAuthToken>();
            builder.Services.AddHttpClient<IGenericHttpWrapperService, Common_GenericHttpWrapperService>();
            builder.Services.AddSingleton<IProcessPlacementsService, ProcessPlacementsService>();
            builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
            builder.Services.AddSingleton<ISapRequestService, SapRequestService>();
            builder.Services.AddSingleton<ITableStorageService, TableStorageService>();
            builder.Services.AddSingleton<IConstructEmployeeObjectService, ConstructEmployeeObjectService>();
            builder.Services.AddSingleton<IUpsertCopStatusService, UpsertCopStatusService>();
            builder.Services.AddSingleton<ISymLibService, SymLibService>();
            builder.Services.AddSingleton<IEventGrid, EventGrid>();
            builder.Services.AddSingleton<IServiceBusHelper, ServiceBusHelper>();
            builder.Services.AddScoped<IStudentPhotoResizeService, StudentPhotoResizeService>();

            var configuration = builder.GetContext().Configuration;
            var studentAppSection = configuration.GetSection(StudentAppOptions.SectionName);

            if (!studentAppSection.Exists())
            {
                studentAppSection = configuration.GetSection($"Values:{StudentAppOptions.SectionName}");
            }

            builder.Services.Configure<StudentAppOptions>(studentAppSection);

            builder.Services.AddSingleton<TokenCredential>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<StudentAppOptions>>().Value;
                var credentialOptions = new DefaultAzureCredentialOptions();

                if (!string.IsNullOrWhiteSpace(options.ManagedIdentityClientId))
                {
                    credentialOptions.ManagedIdentityClientId = options.ManagedIdentityClientId;
                }

                return new DefaultAzureCredential(credentialOptions);
            });

            builder.Services.AddSingleton<IStudentRepository, SqlStudentRepository>();
            builder.Services.AddSingleton<IStudentResponseMapper, StudentResponseMapper>();
            builder.Services.AddSingleton<ISymPayHelper, SymPayHelper>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            var x = builder.ConfigurationBuilder
                .SetBasePath(context.ApplicationRootPath)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
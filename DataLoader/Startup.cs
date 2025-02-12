using DataLoader.Http;
using DataLoader.Repositories;
using DataLoader.Services;
using DataLoader.Services.Import;
using DataLoader.Trees;
using Microsoft.Extensions.DependencyInjection;

namespace DataLoader
{
    class Startup
    {
        public static void ConfigureServices(IServiceCollection services, string apiKey)
        {
            services.AddSingleton<IClient>(sp =>
            {
                return new Client(apiKey);
            });
            services.AddSingleton<ITreeBuilder, TreeBuilder>();
            services.AddSingleton<CustomerRepository>();
            services.AddSingleton<TreeRepository>();
            services.AddSingleton<NodeRepository>();
            services.AddSingleton<CustomerService>();
            services.AddSingleton<VolumeService>();
            services.AddSingleton<SourceGroupRepository>();
            services.AddSingleton<OrderRepository>();
            services.AddSingleton<HistoricalValueRepository>();
            services.AddSingleton<HistoricalBonusRepository>();

            services.AddSingleton<CSVFileReader>();
            services.AddSingleton<ImportManager>();
            services.AddSingleton<NodeImporter>();
            services.AddSingleton<OrderImporter>();
            services.AddSingleton<CustomerImporter>();
            services.AddSingleton<HistoricalValueImporter>();
            services.AddSingleton<HistoricalBonusImporter>();
            services.AddSingleton<SourceImporter>();
        }
    }
}
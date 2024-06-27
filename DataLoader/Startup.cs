using DataLoader.Http;
using DataLoader.Repositories;
using DataLoader.Services;
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
            services.AddSingleton<CustomerService>();
            services.AddSingleton<VolumeService>();
            services.AddSingleton<SourceGroupRepository>();
        }
    }
}
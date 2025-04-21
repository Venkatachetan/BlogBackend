using Microsoft.Extensions.DependencyInjection;
using BlogBackend.Services;
using Supabase;

namespace BlogBackend.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterSupabaseClient(this IServiceCollection services, IConfiguration configuration)
        {
            var supabaseUrl = configuration["Supabase:Url"];
            var supabaseKey = configuration["Supabase:Key"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                throw new InvalidOperationException("Supabase URL and Key must be provided in configuration.");
            }

            services.AddHttpClient();

            services.AddScoped(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(supabaseUrl);
                httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);

                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = true
                };

                return new Supabase.Client(supabaseUrl, supabaseKey, options);
            });

            return services;
        }

        public static IServiceCollection RegisterMongoDbService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<MongoDbService>();
            return services;
        }
    }
}
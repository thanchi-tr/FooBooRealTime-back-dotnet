using FooBooRealTime_back_dotnet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FooBooRealTime_back_dotnet.Configuration
{
    public static class DependancyExtensions
    {
        public static IServiceCollection ConfigureDependancies(this IServiceCollection services, IConfiguration configuration)
        {
            // register the db context
            services.AddDbContext<FooBooDbContext>(
                options => options.UseSqlServer(
                    configuration.GetConnectionString("FooBooStr"),
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,             // Number of retries
                        maxRetryDelay: TimeSpan.FromSeconds(5), // Delay between retries
                        errorNumbersToAdd: null       // SQL error codes that trigger a retry
                ))
                .UseLazyLoadingProxies()
                .EnableDetailedErrors()

            );
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddSwaggerGen(
                opt => {
                    opt.EnableAnnotations(); // enable Swagger Annotation    
                    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "FooBoo Game", Version = "v1" });

                });
            return services;
        }
    }
}

using FooBooRealTime_back_dotnet.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

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
                opt =>
                {
                    opt.EnableAnnotations(); // enable Swagger Annotation    
                    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "FizzBuzz Game", Version = "v1" });
                    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter token",
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        BearerFormat = "JWT",
                        Scheme = "bearer"
                    });

                    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type=ReferenceType.SecurityScheme,
                                    Id="Bearer"
                                }
                            },
                            new string[]{}
                        }
                    });
                }
                
             );

            services.AddSignalR();
            // Authentication
            var auth0Settings = configuration.GetSection("Auth0");
            var authority = auth0Settings["Authority"];
            var audience = auth0Settings["Audience"];
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Log the token for debugging purposes
                            Console.WriteLine($"Token received: {context.Token}");

                            // Allow SignalR access token from query string
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            // Check if the request is for your SignalR hub
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub/game"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };

                });
            services
              .AddAuthorization(options =>
              {
                  options.AddPolicy(
                    "read:user-profile",
                    policy => policy.Requirements.Add(
                      new HasScopeRequirement("read:user-profile", "https://dev-llzbopidy6i26kov.us.auth0.com")
                    )
                  );

                  options.AddPolicy(
                    "delete:user",
                    policy => policy.Requirements.Add(
                      new HasScopeRequirement("delete:user", "https://dev-llzbopidy6i26kov.us.auth0.com")
                    )
                  );
              });
            return services;
        }
    }
}


using backend.Configurations;
using FooBooRealTime_back_dotnet.Configuration;
using FooBooRealTime_back_dotnet.Controllers.SignalR;
using FooBooRealTime_back_dotnet.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizApp.Configurations;
using Serilog;

namespace FooBooRealTime_back_dotnet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(5001, listenOption => // use https only
                {
                    listenOption.UseHttps();
                });
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.AllowAnyClientCertificate();
                });

            });
            Log.Logger = new LoggerConfiguration()
                            .ConfigureLoggerService(builder.Configuration)
                            .CreateLogger();

            builder.Services.ConfigureRegisteredServices();
            builder.Services.ConfigureDependancies(builder.Configuration);
            builder.Services.ConfigureCors();
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                }).AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
                });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Host.UseSerilog();
            try
            {
                Log.Information("Starting up the server...");
                var app = builder.Build();

                // migrate data of DBContext
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<FooBooDbContext>();
                        context.Database.Migrate(); // Apply migrations
                    }
                    catch (Exception ex)
                    {
                        // Log errors during migration
                        app.Logger.LogError(ex, "An error occurred while migrating the database.");
                    }
                }
                // ensure Swagger API not available in production.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseSerilogRequestLogging(); // Logs all requests and responses
                app.UseHttpsRedirection();      // Redirects HTTP to HTTPS
                app.UseRouting();               // Matches routes to endpoints
                app.UseCors("Allow3001");       // Applies CORS policy (choose one)
                app.UseAuthentication();        // Validates credentials and sets User
                app.UseAuthorization();         // Enforces access control policies
                // Map routes and hubs
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHub<GameHub>("hub/game");
                });
                Log.Information("Server successfully started");
                app.Run();

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}

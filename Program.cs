
using backend.Configurations;
using FooBooRealTime_back_dotnet.Configuration;
using FooBooRealTime_back_dotnet.Controllers.SignalR;
using Microsoft.AspNetCore.SignalR;
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
                // ensure Swagger API not available in production.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseSerilogRequestLogging();
                app.UseHttpsRedirection();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
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

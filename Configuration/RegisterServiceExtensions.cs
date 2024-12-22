

using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Interface.Utils;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Services;
using FooBooRealTime_back_dotnet.Services.GameContext;

namespace backend.Configurations
{
    public static class RegisterServiceExtensions
    {
       
        public static IServiceCollection ConfigureRegisteredServices(this IServiceCollection services)
        {
            services.AddSingleton<IGameMaster, GameMaster>();
            services.AddTransient<IGameService, GameService>();
            services.AddTransient<IPlayerService, PlayerService>();
            //services.AddTransient<IRandomIntSource,IRandomIntSource>();
            return services;
        }
    }
}

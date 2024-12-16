using FooBooRealTime_back_dotnet.Interface.Repository;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;

namespace FooBooRealTime_back_dotnet.Interface.Service
{
    public interface IPlayerService : IRepository<Player, PlayerDTO, Guid>
    {
        
    }
}

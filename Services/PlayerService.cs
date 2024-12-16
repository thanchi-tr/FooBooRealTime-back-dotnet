using AutoMapper;
using FooBooRealTime_back_dotnet.Data;
using FooBooRealTime_back_dotnet.Interface.Repository;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Repository;
using Microsoft.EntityFrameworkCore;

namespace FooBooRealTime_back_dotnet.Services
{
    public class PlayerService : Repository<Player, PlayerDTO, Guid>, IPlayerService

    {
        public PlayerService(ILogger<Repository<Player, PlayerDTO, Guid>> logger, FooBooDbContext dbContext, IMapper mapper) : base(logger, dbContext, mapper)
        {
        }

        
    }
}

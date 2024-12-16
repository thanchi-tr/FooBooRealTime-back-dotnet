using AutoMapper;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;

namespace FooBooRealTime_back_dotnet.Utils
{
    public class AutoMappingProfile : Profile
    {
        public AutoMappingProfile()
        {
            
            CreateMap<Player, PlayerDTO>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<PlayerDTO, Player>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Game, GameDTO>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<GameDTO, Game>()
                .ForAllMembers(opts =>
                opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}

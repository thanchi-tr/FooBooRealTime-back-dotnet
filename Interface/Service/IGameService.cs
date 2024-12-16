using FooBooRealTime_back_dotnet.Interface.Repository;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;

namespace FooBooRealTime_back_dotnet.Interface.Service
{
    public interface IGameService : IRepository<Game, GameDTO, string>
    {
        public Task<Boolean> DeleteGameRequest(string gameId, Guid requestor);
        public Task<Game[]> GetAllGameByAuthorId(Guid authorId);

        public Task DeleteAllGameByAuthorId(Guid authorId);

    }
}

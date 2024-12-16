using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Services.GameContext;

namespace FooBooRealTime_back_dotnet.Interface.GameContext
{
    public interface IGameMaster
    {
        public void Refresh(GameDTO changes);
        public Task<Game?> GetContext(string nameId);

        public Task<GameSession?> CreateSessionFromContext(string nameId, SessionPlayer host);


        public GameSession? RetrieveSession(Guid sessionId);

        public GameSession[]? RetiveSessionsByContextName(string nameId);

        public GameSession[] RetrieveActiveSession();
        public SessionPlayer GetActivePlayerDetail(string connectionId);

        public void OnPlayerConnect(string connectionId, SessionPlayer player);

        public GameSession? GetSessionOf(string connectionId);
    }
}

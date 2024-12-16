using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Utils.Validator;
using System.Collections.Concurrent;

namespace FooBooRealTime_back_dotnet.Services.GameContext
{
    public class GameMaster : IGameMaster
    {
        private readonly ConcurrentDictionary<string, Game> _gameContexts = [];
        private readonly ConcurrentDictionary<Guid, GameSession> _activeSession = [];
        private readonly ILogger<IGameMaster> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<string, SessionPlayer> _activePlayers = [];
        private readonly ConcurrentDictionary<string, GameSession> _playerSessions = [];

        public GameMaster(IServiceProvider serviceProvider, ILogger<IGameMaster> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public SessionPlayer GetActivePlayerDetail(string connectionId)
        {
            return _activePlayers[connectionId];
        }

        public void OnPlayerConnect(string connectionId, SessionPlayer player)
        {
            _activePlayers[connectionId] = player;
        }
        /// <summary>
        /// Create a brand new session off of a context with nameId.
        /// [intended to use with signal R <-> host have the connectionId]
        /// </summary>
        /// <param name="nameId"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public async Task<GameSession?> CreateSessionFromContext(string nameId, SessionPlayer host)
        {
            var context = await GetContext(nameId);
            if(context == null || context.Rules.Extract() == null) 
                return null; ;

            var sessionGameData = new SessionGamePlayData(
                    context.Rules.Extract(),
                    context.Range,
                    host.ConnectionId
                );

            var newSession = new GameSession(host, sessionGameData);
            _activeSession[newSession.SessionId] = newSession;
            context.Subscribe(newSession);
            _playerSessions[host.ConnectionId] = newSession;
            return newSession;
        }

        public GameSession? GetSessionOf(string connectionId)
        {
            return _playerSessions[connectionId];
        }

        public GameSession[] RetrieveActiveSession()
        {
            return _activeSession.Values.ToArray();
        }
        public GameSession[]? RetiveSessionsByContextName(string nameId)
        {

            var observers = _gameContexts[nameId].GetObservers();
            return (GameSession[]) observers.Where(o => o.GetType() == typeof(GameSession)).ToArray();

        }
        public GameSession? RetrieveSession(Guid sessionId )
        {
            return _activeSession[sessionId];
        }

        /// <summary>
        /// Return the Game context if exist
        /// </summary>
        /// <param name="nameId"></param>
        /// <returns></returns>
        public async Task<Game?> GetContext(string nameId)
        {

            var target = _gameContexts[nameId];
            if (target == null) // cache missed
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                    target = await gameService.GetByIdAsync(nameId);
                    _gameContexts[target.GameId] = target;
                    _logger.LogInformation($"Game Master: Cache missed, load {target.GameId} into Game master");

                    return target;
                }
            }

            return null;
        }

        public void Refresh(GameDTO changes)
        {
            throw new NotImplementedException();
        }
    }
}

using FooBooRealTime_back_dotnet.Controllers.SignalR;
using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Utils.Validator;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using static Azure.Core.HttpHeader;

namespace FooBooRealTime_back_dotnet.Services.GameContext
{
    public class GameMaster : IGameMaster
    {
        private readonly ConcurrentDictionary<string, Game> _gameContexts = []; // Domain data
        private readonly ConcurrentDictionary<Guid, GameSession> _activeSession = [];
        private readonly ConcurrentDictionary<string, SessionPlayer> _activePlayers = [];
        private readonly ConcurrentDictionary<string, GameSession> _playerSessions = [];

        private readonly ILogger<IGameMaster> _logger;
        private readonly IServiceProvider _serviceProvider;
        public GameMaster(IServiceProvider serviceProvider, ILogger<IGameMaster> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public SessionPlayer? GetActivePlayerDetail(string connectionId)
        {
            if (!_activePlayers.ContainsKey(connectionId))
                return null;
            return _activePlayers[connectionId];
        }

        
        /// <summary>
        /// Handling mapping player (when their connection alter) or create new record
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="player"></param>
        public void OnPlayerConnect(string connectionId, SessionPlayer player)
        {
            if (_activePlayers.ContainsKey(connectionId))
            {
                return;
            }
            
            if (_activePlayers.Where(kpv => kpv.Value.ConnectionId == connectionId).Count() > 0)
            {
                var kpv = _activePlayers.First(kpv => kpv.Value.ConnectionId == connectionId);
                _activePlayers.Remove(kpv.Key, out var _);
                _activePlayers[connectionId] = kpv.Value;
                return;
            }
            // attempt to see if the player is re-connect using a different connectionId
            var target = _activePlayers.FirstOrDefault(kvp => kvp.Value.InternalId == player.InternalId);

            // Remap the new connectionId
            if(!target.Equals(default(KeyValuePair<string, SessionPlayer>)))
            {
                var actionResult = _activePlayers.TryRemove(target);
                if (!actionResult)
                    _logger.LogError("Unexpect behaviour raise : (fail to delete predecated connection Player map)");
            }
            _activePlayers[connectionId] = player;
        }


        public async Task<string[]> RetrieveAllGames()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                    var games = await gameService.GetAllAsync();

                    return games.Select(g => g.GameId).ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Retrieve all games- fail: {ex}");
                return Array.Empty<string>();
            }
        }


        /// <summary>
        /// Create a brand new session off of a context with nameId.
        /// [intended to use with signal R <-> host have the connectionId]
        /// </summary>
        /// <param name="nameId"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public async Task<GameSession> CreateSessionFromContext(string nameId, SessionPlayer host)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope()) // retrieve the Hub 
                {
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();
                    var context = await GetContext(nameId);

                    if (context == null || context.Rules.Extract() == null)
                        throw new HubException("CreateSessiomFromContext: Fail- Game with name {nameId} does not exist");

                    if (host.ConnectionId == null)
                    {
                        throw new HubException("CreateSessiomFromContext: Fail- requestor's connectionId is invalid");
                    }

                    var sessionGameData = new SessionGamePlayData(
                            context.Rules.Extract() ?? new Dictionary<int, string>(),
                            context.Range,
                            host.ConnectionId
                        );
                    var newSession = new GameSession(
                            hubContext,
                        host,
                        sessionGameData,
                        nameId);


                    context.Subscribe(newSession); // alow session to react to change in the game.
                    _activeSession[newSession.SessionId] = newSession;
                    _playerSessions[host.ConnectionId] = newSession;
                    return newSession;
                }
            }
            catch // forward the error to upper level
            {
                throw;
            }
            
        }

        public GameSession? GetSessionOf(string connectionId)
        {
            if(!_playerSessions.ContainsKey(connectionId))
                return null;
            return _playerSessions[connectionId];
        }

        public GameSession[] RetrieveActiveSession()
        {
            return _activeSession.Values.ToArray();
        }
        public GameSession[]? RetiveSessionsByContextName(string nameId)
        {
            if (!_gameContexts.ContainsKey(nameId))
                return [];

            var observers = _gameContexts[nameId].GetObservers();
            return (GameSession[])observers.Where(o => o.GetType() == typeof(GameSession)).ToArray();

        }
        public GameSession? RetrieveSession(Guid sessionId)
        {
            if (!_activeSession.ContainsKey(sessionId))
                return null;
            return _activeSession[sessionId];
        }

        /// <summary>
        /// Return the Game context if exist
        /// </summary>
        /// <param name="nameId"></param>
        /// <returns></returns>
        public async Task<Game?> GetContext(string nameId)
        {
            Game? target = null;
            if (_gameContexts.ContainsKey(nameId))
                target = _gameContexts?[nameId];
            if (target == null) // cache missed
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

                    target = await gameService.GetByIdAsync(nameId);
                    if(target == null || String.IsNullOrEmpty(target.GameId))
                    {
                        throw new ArgumentException($"GameMast:GetContext: namedId with value {nameId} does not exist in system");
                    }
#pragma warning disable CS8602 // Dereference of a possibly null reference., it should not be zero
                    _gameContexts[target.GameId] = target;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    _logger.LogInformation($"Game Master: Cache missed, load <{target.GameId}> into Game master's cache");

                    return target;
                }
            }
            return target;
        }


        public GameSession? OnPlayerLeftSession(string requestorConnectionId)
        {
            // Each player can be connect to one session at a time,
            var requestorSession = _playerSessions[requestorConnectionId];

            if (requestorSession == null)
                return null;
            requestorSession.OnLeftSession(requestorConnectionId);

            // if session is dead (no one left) then dispose it
            if (requestorSession.GetStatus() == SessionStatus.Dead)
            {
                _activeSession.Remove(requestorSession.SessionId, out var disposedSession);
                _logger.LogInformation($"Session: {requestorSession.SessionId} is Dead and Disposed");
            }
            return requestorSession;

        }

        public void CachePlayerSession(string playerConnectionId, Guid sessionId)
        {
            if(!_activeSession.ContainsKey(sessionId) ||
                !_activePlayers.ContainsKey(playerConnectionId))
            {
                return;
            }
            var session = _activeSession[sessionId];
            if(!_playerSessions.ContainsKey(playerConnectionId))
            {
                _playerSessions[playerConnectionId] = session;
            }
        }
        public void Refresh(GameDTO changes)
        {
            throw new NotImplementedException();
        }

        public void UpdateContext(GameDTO changes)
        {
            if (_gameContexts.ContainsKey(changes.GameId))
            {
                var target = _gameContexts[changes.GameId];
                if(changes.Range != target.Range) 
                    target.Range = changes.Range;
                if(changes.Rules != target.Rules) 
                    target.Rules = changes.Rules;

                target.NotifyObservers();

                // notify all idling game about the change
                foreach(var session in _activeSession.Where(s => s.Value.GameName == changes.GameId && s.Value.State() == GameState.WAITING).Select(kvp => kvp.Value).ToList())
                {
                    session.BroadcastUpdate();
                }
            }
            
        }
    }
}

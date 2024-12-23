using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Services.GameContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;

namespace FooBooRealTime_back_dotnet.Controllers.SignalR
{
    //[Authorize]
    public class GameHub : Hub
    {
        private IGameMaster _gameMaster;
        private readonly ILogger<GameHub> _logger;
        private readonly IPlayerService _playerService;

        public GameHub(IGameMaster gameMaster, ILogger<GameHub> logger, IPlayerService playerService)
        {
            _gameMaster = gameMaster;
            _logger = logger;
            _playerService = playerService;
        }

        /// <summary>
        /// Connection to hub onlly occur where player is authenticated 
        /// @not implement the authentication feature
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            var targetId = new Guid("09ac5e84-db5c-4131-0d1c-08dd1c5384cf");
            await base.OnConnectedAsync();
            _logger.LogWarning($"Connection establish: ConnectionID: {Context.ConnectionId}");
            SessionPlayer? sessionPlayer = await SessionPlayer.CreateAsync(targetId, Context.ConnectionId, _playerService);
            if (sessionPlayer == null)
            {
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Please Login in");
                return;
            }
            _gameMaster.OnPlayerConnect(Context.ConnectionId, sessionPlayer);
            _logger.LogWarning($"Connection established: Notify {Context.ConnectionId} About the event.");
            await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, "Player connect to pool");
        }

        /// <summary>
        /// Attempt to join a session with id (supplied)
        /// </summary>
        /// <param name="sessionIdStr"></param>
        /// <returns></returns>
        public async Task JoinSession(string sessionIdStr)
        {
            try
            {
                var sessionId = new Guid(sessionIdStr);

                var activeSession = _gameMaster.RetrieveSession(sessionId);
                if (activeSession != null)
                {
                    activeSession.Join(_gameMaster.GetActivePlayerDetail(Context.ConnectionId));

                    await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Player is connect to Room {sessionIdStr}");
                    await Clients.Caller.SendAsync(ClientMethods.SupplySessionInfo, activeSession.GameName, activeSession.GetRules());
                    await Groups.AddToGroupAsync(Context.ConnectionId, sessionIdStr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"SignalR: catch invalid sessionId{ex}");
            }
        }


        /// <summary>
        /// Allow client to supply their answer (subject to domain logic)
        /// </summary>
        /// <param name="answer"></param>
        public async Task SendAnswer(string answer)
        {
            try
            {
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                if (session == null)
                {
                    await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Player is not in a session");
                    return;
                }
                session?.ProcessAnswer(Context.ConnectionId, answer);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task TogglePlayerReady()
        {
            try
            {
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                if (session == null)
                {
                    await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Player is not in a session");
                    return;
                }
                session.ToggleReady(Context.ConnectionId);
                await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Player {Context.ConnectionId} is Toggling Ready state");
                // also them every one the update infomation about who is ready and who is not
                await Clients.Group(session.SessionId.ToString()).SendAsync(ClientMethods.SupplyScoreBoard, session.GetScoresBoard());
                _logger.LogInformation($"Player {Context.ConnectionId} is Toggling Ready state");

            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }

        public async Task RequestAvailableGameContexts()
        {
            try
            {
                var contexts = await _gameMaster.RetrieveAllGames();
                await Clients.Caller.SendAsync(ClientMethods.SupplyGameContexts, contexts);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }

        public async Task GetAvailableSessions()
        {
            try
            {
                var availableSessions = _gameMaster.RetrieveActiveSession();

                // trigger client RPC to supply the available session
                _logger.LogWarning($"Request Successfull: Grant {Context.ConnectionId} Infomation of Sessions with type {availableSessions.GetType()}.");
                await Clients.Caller.SendAsync(ClientMethods.SupplyAvailableSessions, availableSessions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }

        }



        public async Task RequestScoreBoard()
        {
            try
            {
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                if (session == null)
                {
                    await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Player is not in a session");
                    return;
                }
                var scoreBoard = session?.GetScoresBoard();


                // trigger client RPC to supply the score board
                 await Clients.Caller.SendAsync(ClientMethods.SupplyScoreBoard, scoreBoard);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }


        /// <summary/**/>
        /// A request made to host a new session/
        /// on succeeding
        /// </summary>
        /// <param name="contextNameId"></param>
        public async Task RequestNewSession(string contextNameId)
        {
            var playerSessionDetail = _gameMaster.GetActivePlayerDetail(Context.ConnectionId);
            GameSession? newSession = await _gameMaster.CreateSessionFromContext(contextNameId, playerSessionDetail);

            // trigger client and supply them with the new session
            if (newSession == null)
            {
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Invalid game name");
                return;
            }
            _logger.LogWarning($"Request Successfull: Grant {Context.ConnectionId} access to Created Session {newSession.SessionId}.");
            _logger.LogInformation($"Sending session: {JsonConvert.SerializeObject(newSession)}");
            await Clients.Caller.SendAsync(ClientMethods.SupplySession, newSession);
        }

        public async Task SupplyGameTime (int  gameTime)
        {
            var playerSessionDetail = _gameMaster.GetActivePlayerDetail(Context.ConnectionId);
            var targetSession = _gameMaster.GetSessionOf(Context.ConnectionId);
            if (targetSession != null)
            {
                targetSession.SetGameDurationMinute(gameTime, Context.ConnectionId);
                await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Player {Context.ConnectionId} is setting Game time to {gameTime} min of Session {targetSession.SessionId}");
                _logger.LogInformation($"Player {Context.ConnectionId} is setting Game time to {gameTime} min");
                return;
            }
            _logger.LogInformation($"Player {Context.ConnectionId} is unauthorise to set Game time of Session {targetSession.SessionId}");

        }


        /// <summary>
        /// Handle when player call to leave their respected session
        /// </summary>
        /// <returns></returns>
        public void LeftSession()
        {
            _logger.LogInformation($"Player:: {Context.ConnectionId} request to leave their Current Game Session");
            // delegate task to the game master
            var sessionId = _gameMaster.OnPlayerLeftSession(Context.ConnectionId);

            // we can decide if we want to notify all player in this group that player left
            // for now do nothing
            _logger.LogInformation($"Player:: {Context.ConnectionId} is expelled from Session: {sessionId}");
        }
    }
}

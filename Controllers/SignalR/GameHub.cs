using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Services.GameContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System;

namespace FooBooRealTime_back_dotnet.Controllers.SignalR
{
    [Authorize]
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
            //only need to run for myself therefore hardcode the player
            var targetId = new Guid("09ac5e84-db5c-4131-0d1c-08dd1c5384cf");

            await base.OnConnectedAsync();

            _logger.LogInformation($"Connection Request: requestor ConnectionID: {Context.ConnectionId}");
            SessionPlayer? sessionPlayer = await SessionPlayer.CreateAsync(targetId, Context.ConnectionId, _playerService);
            if (sessionPlayer == null)
            {
                _logger.LogWarning($"Connection Reject: ConnectionID: {Context.ConnectionId} is not authorized");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Error when attempt to connect to hub.");
                throw new HubException("Unauthorized: Only authenticated player is allow to conenct");
            }
            _gameMaster.OnPlayerConnect(Context.ConnectionId, sessionPlayer);
            _logger.LogInformation($"Connection established: Notify {Context.ConnectionId} About the event.");
            await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, "Connection Establish");
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            if (exception == null)
            {
                _logger.LogInformation($"Player with Id:{Context.ConnectionId} is Disconnect");
                return;
            }
            
            _logger.LogError($"Hub:OnDisconnectedAsync: Err: {exception?.Message}");
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
                _logger.LogInformation($"Connection Id: {Context.ConnectionId} Request to Join Session: {sessionIdStr}");
                var sessionId = new Guid(sessionIdStr);
                var activeSession = _gameMaster.RetrieveSession(sessionId);

                if (activeSession == null)
                {
                    throw new HubException($"Connection Id:{Context.ConnectionId} Attempt to access non existed Session");
                }

                var actionResult = activeSession.Join(_gameMaster.GetActivePlayerDetail(Context.ConnectionId));
                if(actionResult)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, sessionIdStr);
                    await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Successful: Player is connected to Room {sessionIdStr}");
                    await Clients.Caller.SendAsync(ClientMethods.SupplySessionInfo, activeSession.GameName, activeSession.GetRules());
                    _logger.LogInformation($"Granted: Request by Connection Id: {Context.ConnectionId} is Successful");
                }
                else
                {
                    await Clients.Caller.SendAsync(ClientMethods.NotifyError, $"Failure:Room {sessionIdStr} reject player connection");
                    _logger.LogInformation($"Reject: Request by Connection Id: {Context.ConnectionId} is unsuccessful");
                }
                
            }
            catch (FormatException ex)
            {
                _logger.LogWarning($"Invalid session Id format: {sessionIdStr}. Error: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Invalid session ID format.");
            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, "An unexpected error occurred.");
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
                    throw new HubException("Un-authorized action:Attempt to call Hub.SendAnswer outside of session scope");
                }
                await session.ProcessAnswer(Context.ConnectionId, answer);
                _logger.LogInformation($"Player with connectionId {Context.ConnectionId} Has made an answer submission");
            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch unexpected {ex}");
            }
        }

        /// <summary>
        /// Toggle player state, if they have a session
        /// </summary>
        public async Task TogglePlayerReady()
        {
            try
            {
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                if (session == null)
                {
                    throw new HubException("Un-authorized action:Attempt to call Hub.TogglePlayerReady outside of session scope");
                }
                await session.ToggleReady(Context.ConnectionId);

            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }

        /// <summary>
        /// Game context: is the Domain data of the game. Session drawn on these data to allow
        /// a runnable version.
        /// </summary>
        /// <returns></returns>
        public async Task RequestAvailableGameContexts()
        {
            try
            {
                var contexts = await _gameMaster.RetrieveAllGames();
                await Clients.Caller.SendAsync(ClientMethods.SupplyGameContexts, contexts);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch Unexpected {ex}");
            }
        }

        public async Task GetAvailableSessions()
        {
            try
            {
                var availableSessions = _gameMaster.RetrieveActiveSession();
                _logger.LogInformation($"Request Successfull: Grant {Context.ConnectionId} Infomation of Sessions with type {availableSessions.GetType()}.");
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
                    throw new HubException("Un-authorized action:Attempt to call Hub.RequestScoreBoard outside of session scope");
                }
                var scoreBoard = session?.GetScoresBoard();


                // trigger client RPC to supply the score board
                await Clients.Caller.SendAsync(ClientMethods.SupplyScoreBoard, scoreBoard);
            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, ex.Message);
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
            try
            {
                var playerSessionDetail = _gameMaster.GetActivePlayerDetail(Context.ConnectionId);

                GameSession newSession = await _gameMaster.CreateSessionFromContext(contextNameId, playerSessionDetail);

                // trigger client and supply them with the new session
                if (newSession == null)
                {
                    throw new HubException("Fail Attempt: cant initiate session.");
                }
                _logger.LogInformation($"Request Successfull: Grant {Context.ConnectionId} access to the Created Session {newSession.SessionId}.");
                _logger.LogInformation($"Sending session: {JsonConvert.SerializeObject(newSession)}");
                await Clients.Caller.SendAsync(ClientMethods.SupplySession, newSession);
            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }

        public async Task SupplyGameTime(int gameTime)
        {
            try
            {
                var playerSessionDetail = _gameMaster.GetActivePlayerDetail(Context.ConnectionId);
                var targetSession = _gameMaster.GetSessionOf(Context.ConnectionId);
                if (targetSession == null)
                {
                    throw new HubException("Un-authorized action:Attempt to call Hub.SupplyGameTime outside of session scope");
                }
                _logger.LogInformation($"Player with connectionId {Context.ConnectionId} request to chagne session: {targetSession.SessionId} time to {gameTime} min");

                var actionResult = await targetSession.SetGameDurationMinute(gameTime, Context.ConnectionId);
                if(!actionResult)
                {
                    throw new HubException("Un-authorized action:Player is prohibit to alter session context.");
                }    
            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }

        }


        /// <summary>
        /// Handle when player call to leave their respected session
        /// </summary>
        /// <returns></returns>
        public async Task LeftSession()
        {
            _logger.LogInformation($"Player:: {Context.ConnectionId} request to leave their Current Game Session");
            // delegate task to the game master
            var sessionId = _gameMaster.OnPlayerLeftSession(Context.ConnectionId);

            // we can decide if we want to notify all player in this group that player left
            // for now do nothing
            _logger.LogInformation($"Player:: {Context.ConnectionId} is expelled from Session: {sessionId}"); 
            await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Player {Context.ConnectionId} is expell");

        }
    }
}

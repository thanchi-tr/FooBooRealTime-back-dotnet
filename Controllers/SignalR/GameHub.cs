using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Services.ExternalApi;
using FooBooRealTime_back_dotnet.Services.GameContext;
using FooBooRealTime_back_dotnet.Utils.Generator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        public GameHub(IGameMaster gameMaster, ILogger<GameHub> logger, IPlayerService playerService) {
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
            Console.WriteLine("\n");
#pragma warning disable CS8604 // Possible null reference argument.
            Guid targetId = Context.ToGuidId();
            var name = Context.GetHttpContext().Request.Query["name"];
#pragma warning restore CS8604 // Possible null reference argument.
            await base.OnConnectedAsync();

            // If the player is an existed player (who has lost connect), remap their connection

            _logger.LogInformation($"Connection Request: requestor ConnectionID: {Context.ConnectionId}");
            SessionPlayer? sessionPlayer = await SessionPlayer.CreateAsync(targetId, Context.ConnectionId, _playerService, name);
            if (sessionPlayer == null)
            {
                _logger.LogWarning($"Connection Reject: ConnectionID: {Context.ConnectionId} is not authorized");
                await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, "Error when attempt to connect to hub.");
                throw new HubException("Unauthorized: Only authenticated player is allow to conenct");
            }
            _gameMaster.OnPlayerConnect(Context.ConnectionId, sessionPlayer);
            _logger.LogInformation($"Connection established: Notify {Context.ConnectionId} About the event.");
            //await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, "Connection Establish");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            _logger.LogInformation($"\n\nConnection Abandoned: Notify {Context.ConnectionId} About the event.");
            if (exception == null)
            {
                _logger.LogInformation($"Player with Id:{Context.ConnectionId} is Disconnect");
                await LeftSession();
                return;
            }
            await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, "Connection Adandone");
            _logger.LogError($"Hub:OnDisconnectedAsync: Err: {exception?.Message}");
        }

        

        public async Task RequestConnectionId()
        {
            await Clients.Caller.SendAsync(ClientMethods.SupplyConnectionId, Context.ConnectionId);
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
                Console.WriteLine("\n");
                // if player is already inside of this session, then supply them the upto date session data.
                var playerSession = _gameMaster.GetSessionOf(Context.ConnectionId);
                
                if (playerSession != null) // use to establish a reconnection on refresh page on room (currently not implement)
                {
                    _logger.LogInformation($"Connection Id: {Context.ConnectionId} Request to reconnect to Session: {sessionIdStr}");
                    _logger.LogInformation($"Connection Id: {Context.ConnectionId} current Session: {sessionIdStr}");
                    //await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Player request to Reconnect to loby of Room:{playerSession.SessionId.ToString()}");
                    if (playerSession.SessionId.ToString()   == sessionIdStr)
                    {
                        //await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Successful: Player is Reconnected to loby of Room:{sessionIdStr}");
                        //await Clients.Groups(playerSession.SessionId.ToString()).SendAsync(ClientMethods.NotifyEvent, $"Welcome!! player: {Context.ConnectionId}");
                        await Clients.Caller.SendAsync(ClientMethods.SupplySessionInfo, playerSession.GameName, playerSession.GetRules(), playerSession.GetHost(), playerSession.GetGameDurationMinute());
                        await Clients.Group(playerSession.SessionId.ToString()).SendAsync(ClientMethods.NotifyReadyStatesChange, playerSession.GetScoresBoard());
                        return;
                    }
                }
                _logger.LogInformation($"Connection Id: {Context.ConnectionId} Request to Join Session: {sessionIdStr}");
                var sessionId = new Guid(sessionIdStr);
                var activeSession = _gameMaster.RetrieveSession(sessionId);

                if (activeSession == null)
                {
                    throw new HubException($"Connection Id:{Context.ConnectionId} Attempt to access non existed Session");
                }
                
                var actionResult = activeSession.Join(_gameMaster.GetActivePlayerDetail(Context.ConnectionId));
                
                if (actionResult)
                {
                    _gameMaster.CachePlayerSession(Context.ConnectionId, sessionId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, activeSession.SessionId.ToString());
                    //await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Successful: Player is connected to loby of Room:{sessionIdStr}");
                    await Clients.Caller.SendAsync(ClientMethods.SupplySessionInfo, activeSession.GameName, activeSession.GetRules(), activeSession.GetHost(), activeSession.GetGameDurationMinute());
                    await Clients.Group(activeSession.SessionId.ToString()).SendAsync(ClientMethods.NotifyReadyStatesChange, activeSession.GetScoresBoard());
                    await Clients.OthersInGroup(activeSession.SessionId.ToString()).SendAsync(ClientMethods.NotifyEvent, $"Welcome!! player: {Context.ConnectionId}");
                    _logger.LogInformation($"Granted: Request by Connection Id: {Context.ConnectionId} is Successful");
                }
                else
                {
                    await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Failure:Room {sessionIdStr} reject player connection");
                    await Clients.Caller.SendAsync(ClientMethods.NotifyRejection);
                    _logger.LogInformation($"Reject: Request by Connection Id: {Context.ConnectionId} is unsuccessful");
                }

            }
            catch (FormatException ex)
            {
                _logger.LogWarning($"Invalid session Id format: {sessionIdStr}. Error: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyRejection);
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, "Invalid session ID format.");
            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyRejection);
                await Clients.Caller.SendAsync(ClientMethods.NotifyError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyRejection);
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
                Console.WriteLine("\n");
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
                Console.WriteLine("\n");
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
                Console.WriteLine("\n");
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
                Console.WriteLine("\n");
                var availableSessions = _gameMaster.RetrieveActiveSession();
                _logger.LogInformation($"Request Successfull: Grant {Context.ConnectionId} Infomation of Sessions.");
                await Clients.Caller.SendAsync(ClientMethods.SupplyAvailableSessions, availableSessions, availableSessions.Select(s => s.GetRules().Count));
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
                Console.WriteLine("\n");
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                if (session == null)
                {
                    await Clients.Caller.SendAsync(ClientMethods.NotifyRejection);
                    throw new HubException("Un-authorized action:Attempt to call Hub.RequestScoreBoard outside of session scope");
                }
                var scoreBoard = session?.GetScoresBoard();


                // trigger client RPC to supply the score board
                await Clients.Caller.SendAsync(ClientMethods.SupplyScoreBoard, scoreBoard);
            }
            catch (HubException ex)
            {
                _logger.LogWarning($"Hub exception: {ex.Message}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyRejection);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
                await Clients.Caller.SendAsync(ClientMethods.NotifyRejection);
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
                Console.WriteLine("\n");
                var playerSessionDetail = _gameMaster.GetActivePlayerDetail(Context.ConnectionId);

                GameSession newSession = await _gameMaster.CreateSessionFromContext(contextNameId, playerSessionDetail);

                // trigger client and supply them with the new session
                if (newSession == null)
                {
                    throw new HubException("Fail Attempt: cant initiate session.");
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, newSession.SessionId.ToString());
                _logger.LogInformation($"Request Successfull: Grant {Context.ConnectionId} access to the Created Session {newSession.SessionId}.");
                _logger.LogInformation($"Sending session: {JsonConvert.SerializeObject(newSession)}");
                await Clients.Caller.SendAsync(ClientMethods.SupplySession, newSession);
                await Clients.Groups(newSession.SessionId.ToString()).SendAsync(ClientMethods.NotifyEvent, $"Session's initiated");
                await Clients.Group(newSession.SessionId.ToString()).SendAsync(ClientMethods.NotifyReadyStatesChange, newSession.GetScoresBoard());

                // notify every one about the newly create game session
                await Clients.All.SendAsync(ClientMethods.SupplyAvailableSessions, _gameMaster.RetrieveActiveSession());
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
                Console.WriteLine("\n");
                var playerSessionDetail = _gameMaster.GetActivePlayerDetail(Context.ConnectionId);
                var targetSession = _gameMaster.GetSessionOf(Context.ConnectionId);
                if (targetSession == null)
                {
                    throw new HubException("Un-authorized action:Attempt to call Hub.SupplyGameTime outside of session scope");
                }
                _logger.LogInformation($"Player with connectionId {Context.ConnectionId} request to chagne session: {targetSession.SessionId} time to {gameTime} min");

                var actionResult = await targetSession.SetGameDurationMinute(gameTime, Context.ConnectionId);
                if (!actionResult)
                {
                    throw new HubException("Un-authorized action:Player is prohibit to alter session context.");
                }
                await Clients.Groups(targetSession.SessionId.ToString()).SendAsync(ClientMethods.NotifyEvent, $"Session game time is setted to be {gameTime}");
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
            Console.WriteLine("\n");
            _logger.LogInformation($"Player:: {Context.ConnectionId} request to leave their Current Game Session");
            // delegate task to the game master

            var session = _gameMaster.OnPlayerLeftSession(Context.ConnectionId);

            // we can decide if we want to notify all player in this group that player left
            // for now do nothing
            //await Clients.Caller.SendAsync(ClientMethods.NotifyEvent, $"Player {Context.ConnectionId} is expell");
            if (session == null)
                return;

            _logger.LogInformation($"Player:: {Context.ConnectionId} is expelled from Session: {session.SessionId}");

            // session is about to be removed, therefore tell every one (not limited to those in the room) about it
            if (session.GetScoresBoard().Length == 0)
            {
                await Clients.All.SendAsync(ClientMethods.SupplyAvailableSessions, _gameMaster.RetrieveActiveSession());
                return;
            }

            // session has player left: notify every one about the player left
            await Clients.Group(session.SessionId.ToString()).SendAsync(ClientMethods.NotifyReadyStatesChange, session.GetScoresBoard());
            await Clients.OthersInGroup(session.SessionId.ToString()).SendAsync(ClientMethods.NotifyEvent, $"player: {Context.ConnectionId} Left Session!");
        }
    }
}

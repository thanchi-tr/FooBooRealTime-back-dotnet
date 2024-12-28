using FooBooRealTime_back_dotnet.Controllers.SignalR;
using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Model.GameContext;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FooBooRealTime_back_dotnet.Services.GameContext
{
    public class GameSession : IObserver
    {

        const int NOT_SET = -1;
        private readonly IHubContext<GameHub> _hubConnection;

        private object _lock = new object();
        public Guid SessionId { get; set; } = Guid.NewGuid();
        private double _gameDurationInMinute = NOT_SET;
        private List<SessionPlayer> _participants { get; set; } = [];
        private SessionGamePlayData _gamePlayData { get; set; }

        public SessionPlayer _host { get; set; }
        public string GameName { get; set; }
        public GameSession(IHubContext<GameHub> hubContext, SessionPlayer host, SessionGamePlayData gamePlayData, string name)
        {
            GameName = name;
            _host = host;
            _gamePlayData = gamePlayData;
            lock (_lock)
            {
                _participants.Add(host);
            }
            _hubConnection = hubContext;
        }

        public Dictionary<int, string> GetRules() => _gamePlayData.Rules;
        public double GetGameDurationMinute() => _gameDurationInMinute;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        public Boolean Join(SessionPlayer player)
        {
            if(player.ConnectionId == null)
            {
                return false;
            }
            var actionResult = _gamePlayData.OnPlayerJoin(player.ConnectionId);
            
            if (actionResult)
            {
                if(!_participants.Contains(player))
                    _participants.Add(player);

            }
            return actionResult;
        }

        public string GetHost() => _host.InternalId.ToString() ?? "not reachable";

        /// <summary>
        /// Handle scenario where player is 
        /// </summary>
        /// <param name="participantConnectionId"></param>
        public void OnLeftSession(string participantConnectionId)
        {
            var target = _participants.Find(p => p.ConnectionId == participantConnectionId);

            if (target == null || target.ConnectionId == null)
                return; // target is not in this session

            _gamePlayData.EraseDataOf(target.ConnectionId);
            _participants.Remove(target);
        }

        /// <summary>
        /// Active: if there some player in room
        /// Dead: empty session and should be dispose
        /// </summary>
        public SessionStatus GetStatus()
        {
            return (_participants.Count > 0)
                ? SessionStatus.Active
                : SessionStatus.Dead;
        }

        public void Disconnect(string playerConnectionId)
        {
            var target = _participants
                        .Find(p => p.ConnectionId == playerConnectionId);
            if (target != null)
                target.IsConnected = false;
        }

        public async void Reconnect(string playerConnectionId, Guid playerId)
        {
            var target = _participants
                            .Find(p => p.InternalId == playerId);
            if (target != null)
            {
                target.IsConnected = true;
                if(target.ConnectionId == null)
                {
                    await _hubConnection.Clients.Client(playerConnectionId).SendAsync(ClientMethods.NotifyError, $"Error occur when attempt to reconnect");
                    return;
                }
                _gamePlayData.OnPlayerReconnect(target.ConnectionId, playerConnectionId);
                target.ConnectionId = playerConnectionId;
            }

        }

        /// <summary>
        /// Condition for game to start:
        ///  (1) . every player is ready
        ///  (2) . game has a time <- this function satisfy 2.
        /// </summary>
        /// <param name="durationInMinute"></param>
        /// <param name="hostId"></param>
        public async Task<Boolean> SetGameDurationMinute(double durationInMinute, string hostId)
        {
            if (_host.ConnectionId != hostId)
            {
                await _hubConnection
                        .Clients.Client(hostId)
                        .SendAsync(ClientMethods.NotifyError, "Attempt to patch un-authorised material");
                return false;
            }
            if(_gamePlayData.CurrentState == GameState.PLAYING)
            {
                await _hubConnection
                        .Clients.Client(hostId)
                        .SendAsync(ClientMethods.NotifyError, "Attempt to change game time mid game");

            }
            if (_gamePlayData.CurrentState != GameState.PLAYING)
            {
                _gameDurationInMinute = durationInMinute;
                await _hubConnection.Clients.Group(SessionId.ToString()).SendAsync(ClientMethods.SupplyGameTime, durationInMinute);
                AttemptToStart();
            }
            return true;
        }

        public GameState State() => _gamePlayData.CurrentState;


        /// <summary>
        /// only viable while game is waiting for all ready to ready (GameState.WAITING)
        /// if all player is ready, it move to game state
        /// 
        /// if positive, start a game loop
        /// </summary>
        /// <returns></returns>
        /// 

        public void AttemptToStart()
        {
            if (_gameDurationInMinute <= 0 || _gamePlayData.CurrentState != GameState.WAITING)
                return;
            foreach (var participant in _gamePlayData.Participants)
            {
                if (!participant.IsReady)
                    return;
            }
            var waitFlag = true;
            // let the game loop continue in the back ground
            try
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                // i want the game loop to run in the back ground and free the server.
                StartGameLoopAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// The Session will automatically switch back to idling state after count down end
        /// </summary>
        /// <returns></returns>
        private async Task StartGameLoopAsync()
        {
            try
            {
                var startTime = DateTime.Now;
                TimeSpan gameDuration = TimeSpan.FromMinutes((int)(_gameDurationInMinute));
                if (_hubConnection == null)
                {
                    throw new HubException("GameSession: Hub reference is missing");
                }                
                _gamePlayData.NextState(); // game loop is about to start
                var initQuestion = GetInitialQuestion();
                await _hubConnection.Clients.Group(SessionId.ToString()).SendAsync(ClientMethods.SupplyInitQuestion, initQuestion);
                await Task.Delay(gameDuration);
                // Notify all clients of game end
                await _hubConnection.Clients.Group(SessionId.ToString()).SendAsync(ClientMethods.NotifyGameEnd);
                _gamePlayData.NextState();
            }catch { 
                throw; 
            }
                    
        }

        /// <summary>
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public async Task<Boolean> ToggleReady(string connectionId)
        {
            var result = false;
            var playerState = false; // default
            foreach (var participant in _gamePlayData.Participants)
            {
                if (participant.playerConnectionId == connectionId)
                {
                    participant.IsReady = !participant.IsReady;
                    if (participant.IsReady)
                    {
                        result = true;
                    }
                    playerState = participant.IsReady;
                    break;
                }
            }
            var readyStateStr = (playerState) ? "Ready" : "Idling";
            await _hubConnection.Clients.Client(connectionId).SendAsync(ClientMethods.NotifyEvent, $"Player {connectionId} is {readyStateStr}");
            // @the group sending is having trouble, opt to the all instead.
            await _hubConnection.Clients.Group(SessionId.ToString()).SendAsync(ClientMethods.NotifyReadyStatesChange, _gamePlayData.Participants); 
            AttemptToStart(); // if this player is the last in the room
            return result;
        }

        /// <summary>
        /// Process the answer and return the update scores
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public async Task<int> ProcessAnswer(string connectionId, string answer)
        {
            var result = _gamePlayData.SubmitAnswer(connectionId, answer);
            
            if(result < 0)
            {
                await _hubConnection.Clients.Client(connectionId).SendAsync(ClientMethods.NotifyError, $"Attempt to submit answer where there are no game loop");
                return result;
            }
            await _hubConnection.Clients.Client(connectionId).SendAsync(ClientMethods.SupplyQuestion, result);
            return result;
        }

        /// <summary>
        /// This method is only accessible during the initial phase,
        /// as a signal where a new game loop is started.
        /// attempt to call any other place will result in an exception
        /// </summary>
        /// <returns></returns>
        public int GetInitialQuestion()
        {
            try
            {
                return _gamePlayData.GetInitQuestion();
            } catch
            {
                throw;
            }
        }
        public PlayerScore[] GetScoresBoard() => _gamePlayData.Participants.ToArray();

        public void Update(GameDTO changes)
        {
            _gamePlayData.Update(changes);
        }

        public void BroadcastUpdate()
        {
            _hubConnection.Clients.Groups(SessionId.ToString()).SendAsync(ClientMethods.SupplySessionInfo, GameName, GetRules(), GetHost(), GetGameDurationMinute());
        }
    };

}

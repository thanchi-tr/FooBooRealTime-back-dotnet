﻿using FooBooRealTime_back_dotnet.Controllers.SignalR;
using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Model.GameContext;
using Microsoft.AspNetCore.SignalR;
using System.Threading;

namespace FooBooRealTime_back_dotnet.Services.GameContext
{
    public class GameSession : IObserver
    {

        private readonly IHubContext<GameHub>? _hubConnection;
        const int NOT_SET = -1;
        private object _lock = new object();
        public Guid SessionId { get; set; } = Guid.NewGuid();
        private double _gameDurationInMinute = NOT_SET;
        private List<SessionPlayer> _participants { get; set; } = [];
        private SessionGamePlayData _gamePlayData { get; set; }

        private SessionPlayer _host { get; set; }

        public GameSession(IHubContext<GameHub> hubContext, SessionPlayer host, SessionGamePlayData gamePlayData)
        {
            _host = host;
            _gamePlayData = gamePlayData;
            lock (_lock)
            {
                _participants.Add(host);
            }
            _hubConnection = hubContext;
        }



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
                _participants.Add(player);

            }

            return actionResult;
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
        public async void SetGameDurationMinute(double durationInMinute, string hostId)
        {
            if (_host.ConnectionId != hostId)
            {
                await _hubConnection
                        .Clients.Client(hostId)
                        .SendAsync(ClientMethods.NotifyError, "Attempt to patch un-authorised material");
                return;
            }
            if(_gamePlayData.CurrentState != GameState.PLAYING)
            {

                await _hubConnection
                        .Clients.Client(hostId)
                        .SendAsync(ClientMethods.NotifyError, "Attempt to change game time mid game");

            }
            if (_gamePlayData.CurrentState != GameState.PLAYING)
            {
                _gameDurationInMinute = durationInMinute;
                AttemptToStart();
            }
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

        public async Task AttemptToStart()
        {
            if (_gameDurationInMinute <= 0 || _gamePlayData.CurrentState != GameState.WAITING)
                return;
            foreach (var participant in _gamePlayData.Participants)
            {
                if (!participant.IsReady)
                    return;
            }
            _gamePlayData.NextState();
            // let the game loop continue in the back ground
            await StartGameLoopAsync();
            return;
        }

        /// <summary>
        /// The Session will automatically switch back to idling state after count down end
        /// </summary>
        /// <returns></returns>
        private async Task StartGameLoopAsync()
        {
            var startTime = DateTime.Now;
            TimeSpan gameDuration = TimeSpan.FromMinutes((int)(_gameDurationInMinute));

            if (_hubConnection != null)
            {
                var initQuestion = GetInitialQuestion();
                await _hubConnection.Clients.All.SendAsync(ClientMethods.SupplyInitQuestion, initQuestion);
            }
            await Task.Delay(gameDuration);

            // Notify all clients of game end
            await _hubConnection.Clients.All.SendAsync(ClientMethods.NotifyGameEnd);

            _gamePlayData.NextState();

        }

        /// <summary>
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public Boolean ToggleReady(string connectionId)
        {
            var result = false;
            foreach (var participant in _gamePlayData.Participants)
            {
                if (participant.playerConnectionId == connectionId)
                {
                    participant.IsReady = !participant.IsReady;
                    if (participant.IsReady)
                    {
                        result = true;
                    }
                }
            }
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
            Console.WriteLine(result);
            if(result < 0)
            {
                await _hubConnection.Clients.Client(connectionId).SendAsync(ClientMethods.NotifyError, $"Attempt to submit answer where there are no game loop");
                return result;
            }
            await _hubConnection.Clients.Client(connectionId).SendAsync(ClientMethods.SupplyQuestion, result);
            return result;
        }

        public int GetInitialQuestion()
        {
            return _gamePlayData.GetInitQuestion();
        }
        public PlayerScore[] GetScoresBoard() => _gamePlayData.Participants.ToArray();

        public void Update(GameDTO changes)
        {
            _gamePlayData.Update(changes);
        }
    };

}

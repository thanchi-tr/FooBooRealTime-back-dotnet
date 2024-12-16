using FooBooRealTime_back_dotnet.Controllers.SignalR;
using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Utils.Validator;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FooBooRealTime_back_dotnet.Services.GameContext
{
    public class GameSession : IObserver
    {
        private readonly IHubContext<GameHub> _hubConnection;
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
        public GameSession(SessionPlayer host, SessionGamePlayData gamePlayData)
        {
            _host = host;
            _gamePlayData = gamePlayData;
            lock (_lock)
            {
                _participants.Add(host);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        public Boolean Join(SessionPlayer player)
        {
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

        public void Reconnect(string playerConnectionId, Guid playerId)
        {
            var target = _participants
                            .Find(p => p.InternalId == playerId);
            if (target != null)
            {
                target.IsConnected = true;
                _gamePlayData.OnPlayerReconnect(target.ConnectionId, playerConnectionId);
                target.ConnectionId = playerConnectionId;
            }

        }

        public void SetGameDurationMinute(double durationInMinute)
        {
            if (_gamePlayData.CurrentState != GameState.PLAYING)
            {
                _gameDurationInMinute = durationInMinute;
            }
            AttemptToStart();
        }
        public GameState State() => _gamePlayData.CurrentState;


        /// <summary>
        /// only viable while game is waiting for all ready to ready (GameState.WAITING)
        /// if all player is ready, it move to game state
        /// 
        /// if positive, start a game loop
        /// </summary>
        /// <returns></returns>
        public void AttemptToStart()
        {
            if (_gameDurationInMinute == NOT_SET || _gamePlayData.CurrentState != GameState.WAITING)
                return;
            foreach (var participant in _gamePlayData.Participants)
            {
                if (!participant.IsReady)
                    return;
            }
            _gamePlayData.NextState();
            // let the game loop continue in the back ground
            StartGameLoopAsync();
            return;
        }

        private async Task StartGameLoopAsync()
        {
            var startTime = DateTime.Now;
            TimeSpan gameDuration = TimeSpan.FromSeconds((int)(_gameDurationInMinute * 60));

            while (DateTime.Now - startTime < gameDuration)
            {
                await Task.Delay(100); // Use Task.Delay instead of Thread.Sleep
            }

            _gamePlayData.NextState(); // Complete game loop
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
        public int ProcessAnswer(string connectionId, string answer)
        {
            return _gamePlayData.SubmitAnswer(connectionId, answer);
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

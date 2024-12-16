using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.GameContext;
using Microsoft.AspNetCore.SignalR;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace FooBooRealTime_back_dotnet.Controllers.SignalR
{
    public class GameHub :Hub
    {
        private IGameMaster _gameMaster;
        private readonly ILogger _logger;
        private readonly IPlayerService _playerService;

        public GameHub(IGameMaster gameMaster, ILogger logger, IPlayerService playerService)
        {
            _gameMaster = gameMaster;
            _logger = logger;
            _playerService = playerService;
        }

        public override async Task OnConnectedAsync()
        {
            var targetId = new Guid("09ac5e84-db5c-4131-0d1c-08dd1c5384cf");
            await base.OnConnectedAsync();
            SessionPlayer sessionPlayer =await SessionPlayer.CreateAsync(targetId, Context.ConnectionId, _playerService);
            _gameMaster.OnPlayerConnect(Context.ConnectionId, sessionPlayer);
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
                    // add the player into a dedicate channel
                    await Groups.AddToGroupAsync(Context.ConnectionId, sessionIdStr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("SignalR: catch invalid sessionId");
            }
        }


        /// <summary>
        /// Allow client to supply their answer (subject to domain logic)
        /// </summary>
        /// <param name="answer"></param>
        public void SendAnswer(string answer)
        {
            try
            {
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                session?.ProcessAnswer(Context.ConnectionId, answer);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void TogglePlayerReady()
        {
            try
            {
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                session.ToggleReady(Context.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }

        public void GetAvailableSessions()
        {
            try
            {
                var availableSessions = _gameMaster.RetrieveActiveSession();

                // trigger client RPC to supply the available session
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }

        }

        public void RequestScoreBoard()
        {
            try
            {
                var session = _gameMaster.GetSessionOf(Context.ConnectionId);
                var scoreBoard = session.GetScoresBoard();

                // trigger client RPC to supply the score board
            }
            catch (Exception ex)
            {
                _logger.LogError($"SignalR: catch {ex}");
            }
        }

        public void RequestNewSession(string contextNameId)
        {
            var playerSessionDetail = _gameMaster.GetActivePlayerDetail(Context.ConnectionId);
            var newSession = _gameMaster.CreateSessionFromContext(contextNameId, playerSessionDetail); ;
            
            // trigger client and supply them with the new session

        }
    }
}

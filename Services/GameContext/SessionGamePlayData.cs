using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Model.GameContext;
using FooBooRealTime_back_dotnet.Utils.Validator;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;


namespace FooBooRealTime_back_dotnet.Services.GameContext
{

    public class SessionGamePlayData
    {
        public const int ERROR = -1;
        private readonly object _lock = new object();
        private Random random = new Random();
        public Dictionary<int, string> Rules { get; private set; }
        protected int _gameRange;
        public GameState CurrentState { get; private set; } = GameState.WAITING;

        protected ConcurrentDictionary<int, string[]> _gameData = [];// ensure a generated number is not repeat
        protected List<int> _gamesQuestionSet = [];
        public List<PlayerScore> Participants = [];

        private GameDTO? _queuedChange = null;
        public SessionGamePlayData(
            Dictionary<int, string> rules,
            int gameRange,
            string hostConnectionId)
        {
            Rules = rules;
            _gameRange = gameRange;
            OnPlayerJoin(hostConnectionId);
        }

        public void Update(GameDTO changes)
        {
            if (CurrentState != GameState.WAITING)
            {
                _queuedChange = changes;
                return;
            }

            var newRule = changes.Rules.Extract();
            if (newRule != null)
            {
                Rules = newRule;
            }
            if (changes.Range > 0)
            {
                _gameRange = changes.Range;
            }
        }

        /// <summary>
        /// Reset previous game state
        /// </summary>
        public void Reset()
        {
            foreach (var player in Participants)
            {
                player.CurrenctQuestionIndex = -1;
                player.CorrectCount = 0;
                player.IsReady = false;
            }
            _gamesQuestionSet.Clear();
            _gameData.Clear();
        }

        public void EraseDataOf(string potentialParticipantConnId)
        {
            var target = Participants.Find(p => p.playerConnectionId == potentialParticipantConnId);

            if (target == null)
                return;

            Participants.Remove(target);
        }

        public void NextState()
        {
            switch (CurrentState)
            {
                case GameState.WAITING:
                    Reset();
                    CurrentState = GameState.PLAYING;
                    break;
                case GameState.PLAYING:
                    // place to do scoring, notify player game end with their performance
                    CurrentState = GameState.WAITING;
                    break;
                default:
                    CurrentState = GameState.WAITING;
                    break;
            }
        }

        /// <summary>
        /// Validate the answer, update the score
        /// 
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public int SubmitAnswer(string connectionId, string answer)
        {
            if (CurrentState != GameState.PLAYING)
                return ERROR;
            // after eval, 
            var target = Participants.Find(p => p.playerConnectionId == connectionId);
            if (target == null)
            {
                // some thing went wrong 
                return ERROR;
            }
            // evaluate the answer and update the score.
            var result = Eval(answer, _gameData[
                                    _gamesQuestionSet[target.CurrenctQuestionIndex] // the question
                                    ]);
            target.CorrectCount += result ? 1 : 0;
            target.CurrenctQuestionIndex++;
            // if the player is @first place.
            if (_gamesQuestionSet.Count <= target.CurrenctQuestionIndex)
            {
                return GenerateNewQuestion();
            }

            return _gamesQuestionSet[target.CurrenctQuestionIndex];
        }

        public int GetInitQuestion()
        {
            foreach (var player in Participants)
            {
                player.CurrenctQuestionIndex++;
            }
            if (_gamesQuestionSet.Count == 0)
                return GenerateNewQuestion();
            else // this is for test file only 
            {
                return _gamesQuestionSet[0];
            }
        }

        /// <summary>
        /// Generate and cache the solution
        /// </summary>
        /// <returns></returns>
        private int GenerateNewQuestion()
        {
            var potential = random.Next(1, _gameRange);
            while (_gameData.ContainsKey(potential))
            {
                potential = random.Next(0, _gameRange);
            }
            _gamesQuestionSet.Add(potential);
            List<string> solutionAnswerSubStr = [];
            foreach (var rule in Rules)
            {
                if (potential % rule.Key == 0)
                    solutionAnswerSubStr.Add(rule.Value);
            }
            _gameData[potential] = solutionAnswerSubStr.ToArray();
            return potential;
        }


        /// <summary>
        /// Only  admit player in if the game is idling
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public bool OnPlayerJoin(string connectionId)
        {
            if (CurrentState == GameState.PLAYING )
            {
                // Notfify the player about this event
                return false;
            }
            if(Participants.Find(p => p.playerConnectionId == connectionId) == null)
                Participants.Add(
                    new PlayerScore { playerConnectionId = connectionId }
                    );
            return true;
        }

        public void OnPlayerReconnect(string connectionId, string newConnectionId)
        {
            var target = Participants
                .Find(p => p.playerConnectionId == connectionId);
            if (target != null)
                target.playerConnectionId = newConnectionId;
        }

        public bool Eval(string answer, string[] expectedSubStrs)
        {
            var expectedSubStrTotalLength = expectedSubStrs.Aggregate(0, (length, str) => length += str.Length);
            if (answer.Length != expectedSubStrTotalLength)
            {
                return false;
            }


            return AttemptSlice(answer, expectedSubStrs);
        }

        public bool AttemptSlice(string str, string[] stringList)
        {
            // base case
            var trimmedStr = str.Trim('\t');
            if (stringList.Length == 0 && trimmedStr.Length == 0) return true;
            if (stringList.Length != 0 && trimmedStr.Length == 0) return false;
            if (stringList.Length == 0 && trimmedStr.Length != 0) return false;
            var exptedStr = stringList[0];
            if (trimmedStr.Length < exptedStr.Length)
            {
                return false;
            }
            string[] subArray = stringList.Skip(1).ToArray();
            // construct the potential str that has str
            List<string> potentialLeftOver = [];
            if (str == exptedStr && subArray.Length == 0)
                return true;
            for (int i = 0; i < str.Length - exptedStr.Length + 1; i++)
            {
                var matchingStr = str.Substring(i, exptedStr.Length);
                if (matchingStr == exptedStr)
                {

                    potentialLeftOver.Add(str[0..i] + "\t" + str[(exptedStr.Length + i)..str.Length]);
                }
            }
            return potentialLeftOver.Count == 0
                ? false // an expected sub str not found: must be false
                : potentialLeftOver
                // any expression tree yield a true.
                .Aggregate(false, (predicate, cur) =>
                    predicate || AttemptSlice(cur, subArray)
                );
        }
    }
}

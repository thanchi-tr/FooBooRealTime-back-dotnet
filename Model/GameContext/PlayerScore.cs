namespace FooBooRealTime_back_dotnet.Model.GameContext
{
    public class PlayerScore
    {
        public string playerConnectionId { get; set; }
        public int CurrenctQuestionIndex { get; set; } = -1;
        public int CorrectCount { get; set; } = 0;
        public Boolean IsReady { get; set; } = false;
        public Boolean IsDisconnect { get; set; } = false;
    }
}

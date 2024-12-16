namespace FooBooRealTime_back_dotnet.Model.GameContext
{
    public class PlayerScore
    {
        public string playerConnectionId;
        public int CurrenctQuestionIndex = -1;
        public int CorrectCount = 0;
        public Boolean IsReady = false;
    }
}

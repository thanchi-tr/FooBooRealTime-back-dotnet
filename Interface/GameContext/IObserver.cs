using FooBooRealTime_back_dotnet.Model.DTO;

namespace FooBooRealTime_back_dotnet.Interface.GameContext
{
    public interface IObserver
    {
        public void Update(GameDTO changes);
    }
}

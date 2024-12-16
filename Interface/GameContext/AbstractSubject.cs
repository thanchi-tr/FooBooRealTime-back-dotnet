using System.ComponentModel.DataAnnotations.Schema;

namespace FooBooRealTime_back_dotnet.Interface.GameContext
{
    public abstract class AbstractSubject
    {
        [NotMapped]
        protected List<IObserver> Observers = [];

        public void Subscribe(IObserver observer)
        {
            Observers.Add(observer);
        }

        public void Unsubscribe(IObserver observer)
        {
            Observers.Remove(observer);
        }

        public IObserver[] GetObservers() => Observers.ToArray();
        public abstract void NotifyObservers();
    }
}

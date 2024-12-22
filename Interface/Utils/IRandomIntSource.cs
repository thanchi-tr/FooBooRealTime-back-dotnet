namespace FooBooRealTime_back_dotnet.Interface.Utils
{
    public interface IRandomIntSource
    {
        public int Generate(int sourceUpperBound);
    }
}

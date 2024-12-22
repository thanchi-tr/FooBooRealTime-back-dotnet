using FooBooRealTime_back_dotnet.Interface.Utils;

namespace FooBooRealTime_back_dotnet.Utils.Generator
{
    /// <summary>
    /// This class role is only to generate random positive integer number
    /// </summary>
    public class RandomIntSource : IRandomIntSource
    {
        private readonly Random _random = new Random();

        public RandomIntSource()
        {
        }

        public int Generate(int sourceUpperBound)
        {
            return _random.Next(1, sourceUpperBound);
        }
    }
}

using System.Collections.Concurrent;

namespace FooBooRealTime_back_dotnet.Utils.Generator
{
    public static class AnswerCompositionGenerator
    {
        public static ConcurrentDictionary<int, string[]> GenerateAnswerComposition(
            this ConcurrentDictionary<int, string[]> answerCache, 
            Dictionary<int, string> Rules, 
            int question)
        {
            if (question == 0)
            {
                answerCache[question] = Array.Empty<string>(); // invalid
                return answerCache;
            }
            List<string> solutionAnswerSubStr = [];
            foreach (var rule in Rules)
            {
                if (question % rule.Key == 0)
                    solutionAnswerSubStr.Add(rule.Value);
            }
            answerCache[question] = solutionAnswerSubStr.ToArray();

            return answerCache;
        }

    }
}

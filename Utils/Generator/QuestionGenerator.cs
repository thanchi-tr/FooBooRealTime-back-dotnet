using FooBooRealTime_back_dotnet.Interface.Utils;
using System;
using System.Collections.Concurrent;

namespace FooBooRealTime_back_dotnet.Utils.Generator
{
    public static class QuestionGenerator
    {

        /// <summary>
        /// Create a new quesition using context inject into the method
        ///  - the new question will be from 1 - the questionBoundy(maximum)
        /// </summary>
        /// <param name="questionSet"></param>
        /// <param name="Rules"></param>
        public static void Generate(this List<int> questionSet,
                int questionBoundary,
                IRandomIntSource randomSource)
        {
            var potential = randomSource.Generate(questionBoundary);
            while (questionSet.Contains(potential))
            {
                potential = randomSource.Generate(questionBoundary);
            }
            questionSet.Add(potential);
        }
    }
}

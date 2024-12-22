using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace FooBooRealTime_back_dotnet.Utils.Validator {


    public static class AnswerValidation 
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nonEvaluatedAnswer"></param>
        /// <param name="expectedSubStrs"></param>
        /// <returns></returns>
        public static bool Evaluate( this string nonEvaluatedAnswer, string[] expectedSubStrs)
        {
            var expectedSubStrTotalLength = expectedSubStrs.Aggregate(0, (length, str) => length += str.Length);
            if (nonEvaluatedAnswer.Length != expectedSubStrTotalLength)
            {
                return false;
            }
            return EvaluateSubString(nonEvaluatedAnswer, expectedSubStrs);
        }

        /// <summary>
        /// Attempt to match this word as any combination of the string list
        ///     e.g: for expectedCompositions ["Hai", "Bon"] the baseStr can be
        ///         "HaiBon" or "BonHai" . Either match result in a true
        /// </summary>
        /// <param name="baseStr"></param>
        /// <param name="expectedCompositions"> contain a list of substr that each one is expected to be in baseStr</param>
        /// <returns></returns>
        private static bool EvaluateSubString(string baseStr, string[] expectedCompositions)
        {
            // base case
            var trimmedStr = baseStr.Trim('\t');
            if (expectedCompositions.Length == 0 && trimmedStr.Length == 0) return true;
            if (expectedCompositions.Length != 0 && trimmedStr.Length == 0) return false;
            if (expectedCompositions.Length == 0 && trimmedStr.Length != 0) return false;
            var exptedStr = expectedCompositions[0];
            if (trimmedStr.Length < exptedStr.Length)
            {
                return false;
            }
            string[] subArray = expectedCompositions.Skip(1).ToArray();
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
                    predicate || EvaluateSubString(cur, subArray)
                );
        }
    }
}
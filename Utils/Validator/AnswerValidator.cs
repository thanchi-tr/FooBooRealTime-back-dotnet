namespace FooBooRealTime_back_dotnet.Utils.Validator
{
    public static class AnswerValidator
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nonEvaluatedAnswer"></param>
        /// <param name="expectedSubStrs"></param>
        /// <returns></returns>
        public static bool IsComposeOf(this string nonEvaluatedAnswer, string[] expectedSubStrs)
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
        public static bool EvaluateSubString(string str, string[] stringList)
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
                    predicate || EvaluateSubString(cur, subArray)
                );
        }
    }
}

using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace FooBooRealTime_back_dotnet.Utils.Validator
{

    /// <summary>
    /// Rule:
    /// key: is a number that if a questioned number was to divisible by this key, the val is expected in the answer.
    /// val: string
    /// 
    /// Game context: has arange,
    /// </summary>
    public static class RuleValidator
    {
        
        public static Dictionary<int, string>? Extract(this string gameRules)
        {
            var rules = gameRules;
            if (string.IsNullOrEmpty(rules))
                return null;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {

                var deserialisedRule = JsonSerializer.Deserialize<Rules>(rules, options);
                Dictionary<int, string> dict = [];
                if (deserialisedRule == null)
                    return null; 

                foreach(var rule in deserialisedRule.RuleList)
                { 
                    if(rule.Key <= 0)
                        return null;
                    if(dict.ContainsKey(rule.Key))
                        return null; // ambigiousty

                    dict[rule.Key] = rule.Value;
                }
                return dict;
            }
            catch (Exception _)
            {
            }

            return null;
        }

        public static Boolean Validate(this string gameRules)
        {
            return gameRules.Extract() != null; 
        }
    }
}

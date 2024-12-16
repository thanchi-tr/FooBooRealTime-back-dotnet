using backend.Model.LogUtil;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FooBooRealTime_back_dotnet.Model.Domain
{
    public class Player : IHasStringId
    {
        public Guid PlayerId { get; set; }
        public string Name { get; set; }

        // Relationship
        [JsonIgnore]
        public virtual ICollection<Game> CreatedGames { get; set; }


        // irrelevant logging property
        [NotMapped]
        public string IdToString => PlayerId.ToString();
    }
}

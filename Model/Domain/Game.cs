using AutoMapper;
using backend.Model.LogUtil;
using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Model.DTO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FooBooRealTime_back_dotnet.Model.Domain
{
    public class Game : 
        AbstractSubject, // which make this as a context where the session listen for change from
        IHasStringId
    {
        [Key]
        public string GameId {  get; set; } //which is a unique name
        [Required]
        public int Range { get; set; }
        [Required]
        public string Rules { get; set; } // stringify Jonson <-> {key:string, value:string}[]


        // Relationship
        public Guid? AuthorId { get; set; }
        [JsonIgnore]
        public virtual Player Author { get; set; }


        // irrelevant logging property
        [NotMapped, JsonIgnore]
        public string IdToString => GameId;
        
        

        public override void NotifyObservers()
        {
            var gameDto = new GameDTO { AuthorId = AuthorId, GameId =GameId,Range = Range, Rules = Rules };
            foreach(var obs  in Observers)
            {
                obs.Update(gameDto);
            }
        }
    }
}

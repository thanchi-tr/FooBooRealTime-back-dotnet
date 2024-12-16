using System.ComponentModel.DataAnnotations;
using System.Data;

namespace FooBooRealTime_back_dotnet.Model.DTO
{
    public class GameDTO
    {
        public Guid? AuthorId { get; set; }
        public string GameId { get; set; } //which is a unique name
        public int Range { get; set; }
        public string Rules { get; set; }

    }
}

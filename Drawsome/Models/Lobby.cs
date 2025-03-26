using System.ComponentModel.DataAnnotations;

namespace Drawsome.Models
{
    public class Lobby
    {
        [Required, StringLength(20)]
        public string LobbyName { get; set; }
        public List<string> Players { get; set; } = new List<string>();
    }
}
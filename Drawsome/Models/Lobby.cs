namespace Drawsome.Models
{
    public class Lobby
    {
        public string LobbyName { get; set; }
        public List<string> Players { get; set; } = new List<string>();
        public string Creator { get; set; }
    }
}
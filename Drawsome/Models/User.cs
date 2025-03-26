using System.ComponentModel.DataAnnotations;

namespace Drawsome.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required, StringLength(50)]
        public string Username { get; set; }
        
        [Required, StringLength(100)]
        public string Password { get; set; }
        public int Score { get; set; }
        public bool IsAdmin { get; set; }
    }
}
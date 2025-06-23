using System.ComponentModel.DataAnnotations;

namespace Drawsome.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        public string Username { get; set; }
        
        [Required]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must be between 12 and 128 characters long")]
        public string Password { get; set; }
        public int Score { get; set; }
        public bool IsAdmin { get; set; }
    }
}
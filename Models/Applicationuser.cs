using Microsoft.AspNetCore.Identity;

namespace Finance_Tracker.Models
{
    public class Applicationuser : IdentityUser
    {
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}

namespace Finance_Tracker.Models
{
    public class UserBudget
    {
        public int Id { get; set; }

        // Foreign Key for Category
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        // Foreign Key for User
        public string UserId { get; set; }
        public Applicationuser User { get; set; }

        // User-specific budget for this category
        public decimal? Budget { get; set; }

        public string? BudgetforMonth {  get; set; }
    }
}

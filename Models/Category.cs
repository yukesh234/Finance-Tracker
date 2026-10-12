namespace Finance_Tracker.Models
{
    public enum CategoryType
    {
        Income,
        Expense
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Icon { get; set; }
        public CategoryType CategoryType { get; set; }

        // Foreign key for user-defined categories
        public string? UserId { get; set; }
        public Applicationuser? Applicationuser { get; set; }

        // Transactions associated with this category
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        // User-specific budgets associated with this category
        public ICollection<UserBudget> UserBudgets { get; set; } = new List<UserBudget>();
    }
}

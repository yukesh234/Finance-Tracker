using Finance_Tracker.Models;

namespace Finance_Tracker.ViewModel
{
    public class BudgetViewModel
    {
       public int? id { get; set; }
        public List<Category>? Categories;
        public decimal Amount {  get; set; }
        public Dictionary<int, decimal> Expenses { get; set; } = new();
        public List<UserBudget>? Budgets;
        public int? CategoryId { get; set; }

        public string? BudgetforMonth { get; set; }
    }
}

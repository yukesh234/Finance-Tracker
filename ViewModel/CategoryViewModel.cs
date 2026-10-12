using Finance_Tracker.Models;

namespace Finance_Tracker.ViewModel
{
    public enum CategoryType
    {
        Income,
        Expense
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Icon { get; set; }
        public CategoryType CategoryType { get; set; }
        public List<Category> Categories { get; set; }
    }
}
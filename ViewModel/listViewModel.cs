using Finance_Tracker.Models;

namespace Finance_Tracker.ViewModel
{
    public class listViewModel
    {
        public List<Transaction> Transactions { get; set; } = new List<Models.Transaction> { };
        public string? CategoryName { get; set; }

    }
}

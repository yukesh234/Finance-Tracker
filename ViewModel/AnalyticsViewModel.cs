using System.ComponentModel.DataAnnotations;
using Finance_Tracker.Models;

namespace Finance_Tracker.ViewModel
{
    public enum newCategoryType
    {
        Expense,
        Income
        
    }
    public class AnalyticsViewModel
    {
    
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty; 
            public int Transactions { get; set; }

           [Required(ErrorMessage ="Name is required")]
            public string NewCategoryName {  get; set; } = string.Empty;

            [Required(ErrorMessage ="Icon cannot be empty")]
            public string NewIcon { get; set; }
            
            public newCategoryType newCategoryType {  get; set; }


    }
}

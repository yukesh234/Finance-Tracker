namespace Finance_Tracker.Models
{
   
        public enum TransactionType
        {
            Income,
            Expense
        }
        public enum RecurrenceInterval
        {
            None,
            Daily,
            Weekly,
            Monthly,
            Yearly
        }

        // Transaction Model
        public class Transaction
        {
            public int Id { get; set; }
        public string? Name { get; set; }

            public decimal Amount { get; set; }
            public DateTime Date { get; set; } = DateTime.Now;
            public string Description { get; set; }

            public TransactionType TransactionType { get; set; }

            // Foreign Key for Category
            public int CategoryId { get; set; }
            public Category Category { get; set; }
            //recurring 
            public bool IsRecurring { get; set; } = false;

            public RecurrenceInterval? RecurrenceInterval { get; set; } 

            // Foreign Key for ApplicationUser
            public string UserId { get; set; }
            public Applicationuser Applicationuser { get; set; }
            public DateTime? Deducteddate { get; set; }
    }
    }

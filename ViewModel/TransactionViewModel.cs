using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Transactions;

namespace Finance_Tracker.ViewModel
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

    public class TransactionViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; } = DateTime.Now;

        public bool IsRecurring { get; set; } = false;

        public RecurrenceInterval? RecurrenceInterval { get; set; } 

        public TransactionType? Type { get; set; }
        public string? Name { get; set; }

        // Dropdown for categories
        public List<SelectListItem> Categories { get; set; } = new();
        //Transaction for reading 
        public List<Finance_Tracker.Models.Transaction> Transactions { get; set; } = new List<Models.Transaction> { };


        


    }
}

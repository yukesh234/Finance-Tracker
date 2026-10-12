using Finance_Tracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Finance_Tracker.Data
{
    public class ApplicationDbcontext : IdentityDbContext<Applicationuser>

    {
        public ApplicationDbcontext(DbContextOptions options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserBudget> Budgets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Salary", Icon = "💼", CategoryType = CategoryType.Income },
                new Category { Id = 2, Name = "Freelance", Icon = "🧑‍💻", CategoryType = CategoryType.Income },
                new Category { Id = 3, Name = "Food", Icon = "🍕", CategoryType = CategoryType.Expense },
                new Category { Id = 4, Name = "Transport", Icon = "🚗", CategoryType = CategoryType.Expense },
                new Category { Id = 5, Name = "Entertainment", Icon = "🎮", CategoryType = CategoryType.Expense },
                new Category { Id = 6, Name = "Health & Medicine", Icon = "🧑‍⚕️", CategoryType = CategoryType.Expense },
                new Category { Id = 7, Name = "Receivables", Icon = "💰", CategoryType = CategoryType.Income },
                new Category { Id = 8, Name = "Payables", Icon = "💸", CategoryType = CategoryType.Expense },
                new Category { Id = 9, Name = "Stocks and Investment", Icon = "📈", CategoryType = CategoryType.Income },
                new Category { Id = 10, Name = "Education", Icon = "🎓", CategoryType = CategoryType.Expense },
                new Category
                {
                    Id = 11,
                    Name = "Shopping",
                    Icon = "🛍️",
                    CategoryType = CategoryType.Expense
                },
                new Category
                {
                    Id = 12,
                    Name = "Bonus",
                    Icon = "💵",
                    CategoryType = CategoryType.Income
                }
            );
        }
    }
}

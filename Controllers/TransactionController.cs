using System.Security.Claims;
using Finance_Tracker.Data;
using Finance_Tracker.Models;
using Finance_Tracker.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;

namespace Finance_Tracker.Controllers
{
    public class TransactionController : Controller
    {
        protected readonly ApplicationDbcontext _context;

        public TransactionController(ApplicationDbcontext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Transaction(string? selection)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch categories
            var categories = await _context.Categories
                .Where(c => c.UserId == null || c.UserId == userId)
                .ToListAsync();

            // Base transaction query
            var transactionsQuery = _context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId);

            // Filter by TransactionType if valid selection is made
            if (Enum.TryParse<Models.TransactionType>(selection, out var selectedType))
            {
                transactionsQuery = transactionsQuery.Where(t => t.TransactionType == selectedType);
            }

            var transactions = await transactionsQuery
                .OrderByDescending(t => t.Date)
                .Take(500)
                .ToListAsync();

            var viewModel = new TransactionViewModel
            {
                Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Group = new SelectListGroup { Name = c.CategoryType.ToString() }
                }).ToList(),

                Transactions = transactions
            };
            ViewBag.Selection = selection;
            return View(viewModel);
        }


        public async Task<IActionResult> Dashboard(string? selectedMonth)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var AutoTransactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            DateTime now = DateTime.Now;

            foreach (var t in AutoTransactions.Where(t => t.IsRecurring))
            {
                var lastDeducted = t.Deducteddate ?? t.Date;
                var transactionsToAdd = new List<Transaction>();

                // Calculate how many periods have elapsed
                DateTime currentPeriodStart = lastDeducted;

                while (ShouldCreateTransaction(currentPeriodStart, now, (Models.RecurrenceInterval)t.RecurrenceInterval))
                {
                    currentPeriodStart = GetNextPeriodDate(currentPeriodStart, (Models.RecurrenceInterval)t.RecurrenceInterval);

                    var recurringEntry = new Transaction
                    {
                        UserId = t.UserId,
                        Amount = t.Amount,
                        CategoryId = t.CategoryId,
                        TransactionType = t.TransactionType,
                        Description = $"[Recurring] {t.Description}",
                        Date = currentPeriodStart,
                        IsRecurring = false,
                        RecurrenceInterval = Models.RecurrenceInterval.None,
                        Deducteddate = null
                    };

                    transactionsToAdd.Add(recurringEntry);
                }

                // Add all transactions at once
                if (transactionsToAdd.Any())
                {
                    _context.Transactions.AddRange(transactionsToAdd);
                    // Update last deducted date to the most recent transaction date
                    t.Deducteddate = transactionsToAdd.Last().Date;
                }
            }

            await _context.SaveChangesAsync();

            // Manually defined list of months
            var months = new List<string>
{
    "January 2025",
    "February 2025",
    "March 2025",
    "April 2025",
    "May 2025",
    "June 2025",
    "July 2025",
    "August 2025",
    "September 2025",
    "October 2025", // corrected capitalization
    "November 2025",
    "December 2025"
};
            ViewBag.Months = months;
            ViewBag.SelectedMonth = selectedMonth;

            // Step 1: Parse the selected month
            DateTime monthStart = DateTime.MinValue;
            DateTime monthEnd = DateTime.MaxValue;
            if (!string.IsNullOrEmpty(selectedMonth))
            {
                DateTime.TryParseExact("01-" + selectedMonth, "dd-MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out monthStart);
                monthEnd = monthStart.AddMonths(1).AddDays(-1); // end of month
            }

            // Step 2: Last 7 days for spline chart only
            var startDate = DateTime.Now.AddDays(-7);
            var endDate = DateTime.Now.AddDays(1);

            var transactionsForSpline = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                .ToListAsync();

            // Step 3: Monthly data for rest of the dashboard
            var monthlyTransactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Category)
                .Where(t => t.UserId == userId && t.Date >= monthStart && t.Date <= monthEnd)
                .ToListAsync();

            // Donut chart data
            var expenseByCategory = monthlyTransactions
                .Where(t => t.TransactionType == Models.TransactionType.Expense && t.Category != null)
                .GroupBy(t => t.Category.Name)
                .Select(group => new
                {
                    Categorywithicon = group.First().Category.Icon + " " + group.Key,
                    amount = group.Sum(t => t.Amount),
                    formattedamount = group.Sum(t => t.Amount).ToString("C0", new CultureInfo("ne-NP") { NumberFormat = { CurrencySymbol = "रु" } })
                }).ToList();
            ViewBag.DoughnutChart = expenseByCategory;

            // Overall summary
            decimal income = monthlyTransactions
                .Where(t => t.TransactionType == Models.TransactionType.Income)
                .Sum(t => t.Amount);
            decimal expense = monthlyTransactions
                .Where(t => t.TransactionType == Models.TransactionType.Expense)
                .Sum(t => t.Amount);
            decimal balance = income - expense;

            CultureInfo nepalCulture = new CultureInfo("ne-NP");
            nepalCulture.NumberFormat.CurrencySymbol = "रु";

            ViewBag.income = income.ToString("C0", nepalCulture);
            ViewBag.expense = expense.ToString("C0", nepalCulture);
            ViewBag.balance = balance.ToString("C0", nepalCulture);

            // Spline chart (last 7 days)
            List<SplineChartData> IncomeSummary = transactionsForSpline
              .Where(i => i.TransactionType == Models.TransactionType.Income)
              .GroupBy(i => i.Date)
              .Select(k => new SplineChartData()
              {
                  day = k.First().Date.ToString("dd-MMM"),
                  income = (int)k.Sum(t => t.Amount),
              }).ToList();

            List<SplineChartData> ExpenseSummary = transactionsForSpline
             .Where(i => i.TransactionType == Models.TransactionType.Expense)
             .GroupBy(i => i.Date)
             .Select(k => new SplineChartData()
             {
                 day = k.First().Date.ToString("dd-MMM"),
                 expense = (int)k.Sum(t => t.Amount),
             }).ToList();

            string[] Last7Days = Enumerable.Range(0, 7)
              .Select(i => startDate.AddDays(i).ToString("dd-MMM"))
              .ToArray();

            ViewBag.SplineChartData = from day in Last7Days
                                      join incomeData in IncomeSummary on day equals incomeData.day into dayIncomeJoined
                                      from incomeItem in dayIncomeJoined.DefaultIfEmpty(new SplineChartData { day = day, income = 0 })
                                      join expenseData in ExpenseSummary on day equals expenseData.day into expenseJoined
                                      from expenseItem in expenseJoined.DefaultIfEmpty(new SplineChartData { day = day, expense = 0 })
                                      select new
                                      {
                                          day = day,
                                          income = incomeItem.income,
                                          expense = expenseItem.expense,
                                      };

            return View();

        }
        private bool ShouldCreateTransaction(DateTime lastDate, DateTime now, Models.RecurrenceInterval interval)
        {
            return GetNextPeriodDate(lastDate, interval) <= now;
        }

        private DateTime GetNextPeriodDate(DateTime currentDate, Models.RecurrenceInterval interval)
        {
            return interval switch
            {
                Models.RecurrenceInterval.Daily => currentDate.AddDays(1),
                Models.RecurrenceInterval.Weekly => currentDate.AddDays(7),
                Models.RecurrenceInterval.Monthly => currentDate.AddMonths(1),
                Models.RecurrenceInterval.Yearly => currentDate.AddYears(1),
                _ => currentDate
            };
        }

        public async Task<IActionResult> Budget()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfNextMonth = startOfMonth.AddMonths(1);
            string currentMonthKey = startOfMonth.ToString("MMMM yyyy");

            var budgets = await _context.Budgets
                .Include(ub => ub.Category)
                .Where(ub => ub.UserId == userId && ub.BudgetforMonth == currentMonthKey)
                .ToListAsync();

            var expenses = await _context.Transactions
                .Where(t => t.UserId == userId
                            && t.TransactionType == Models.TransactionType.Expense
                            && t.Date >= startOfMonth
                            && t.Date < startOfNextMonth)
                .GroupBy(t => t.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    TotalSpent = g.Sum(t => t.Amount)
                })
                .ToDictionaryAsync(x => x.CategoryId, x => x.TotalSpent);

            // GET ALL categories, regardless of budgeting
            var allCategories = await _context.Categories
                .Where(c => (c.UserId == userId || c.UserId == null)
                            && c.CategoryType == Models.CategoryType.Expense)
                .ToListAsync();

            var viewModel = new BudgetViewModel
            {
                Categories = allCategories,   // No filtering here
                Budgets = budgets,
                Expenses = expenses,
                BudgetforMonth = currentMonthKey
            };

            return View(viewModel);
        }


        public async Task<IActionResult> Analytics(string? selectedMonth)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Manually defined list of months
            var months = new List<string>
           {
        "January 2025",
        "February 2025",
        "March 2025",
        "April 2025",
        "May 2025",
        "June 2025",
        "July 2025",
        "August 2025",
        "September 2025",
        "OCtober 2025",
        "November 2025",
        "December 2025"
        
             };

            ViewBag.Months = months;
            ViewBag.SelectedMonth = selectedMonth;

            // Fetch relevant categories (Expense only)
            var categories = await _context.Categories
                .Where(c => (c.UserId == userId || c.UserId == null) && c.CategoryType == Models.CategoryType.Expense)
                .ToListAsync();

            // Fetch all transactions for the user
            var transactionsQuery = _context.Transactions.Where(t => t.UserId == userId);

            // Apply month filter if selected
            if (!string.IsNullOrEmpty(selectedMonth))
            {
                var parsedMonth = DateTime.ParseExact(selectedMonth, "MMMM yyyy", null);
                var startOfMonth = new DateTime(parsedMonth.Year, parsedMonth.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                transactionsQuery = transactionsQuery
                    .Where(t => t.Date >= startOfMonth && t.Date < endOfMonth);
            }

            var transactions = await transactionsQuery.ToListAsync();

            // Group transactions by category
            var analyticsData = categories.Select(category => new AnalyticsViewModel
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Icon = category.Icon,
                Transactions = transactions.Count(t => t.CategoryId == category.Id)
            }).ToList();

            return View(analyticsData);
        }



        [HttpPost]
        public async Task<IActionResult> AddBudget(BudgetViewModel model, int CategoryId)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var userbudget = new UserBudget
                {
                    Budget = model.Amount,
                    CategoryId = CategoryId,
                    UserId = userId,
                    BudgetforMonth = DateTime.UtcNow.ToString("MMMM yyyy")
                };

                _context.Budgets.Add(userbudget);
                await _context.SaveChangesAsync();
                Console.WriteLine($"{userbudget.Budget} {userbudget.CategoryId} {userbudget.BudgetforMonth}");
                TempData["Success"] = "Budget added successfully!";
                return RedirectToAction("Budget");
            }
            

            var userIdReload = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.Categories = await _context.Categories
                .Where(c => (c.UserId == userIdReload || c.UserId == null) && c.CategoryType == Models.CategoryType.Expense)
                .ToListAsync();
            model.Budgets = await _context.Budgets
                .Include(ub => ub.Category)
                .Where(ub => ub.UserId == userIdReload)
                .ToListAsync();
            return View("Budget", model);
        }

        [HttpPost]
        public async Task<IActionResult> Addcategory(AnalyticsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var category = new Category
                {
                    UserId = userId,
                    Icon = model.NewIcon,
                    Name = model.NewCategoryName,
                    CategoryType = (Models.CategoryType)model.newCategoryType
                };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction("Analytics");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddTransaction(TransactionViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ModelState.IsValid)
            {
               

                var transaction = new Transaction
                {
                    Amount = model.Amount,
                    CategoryId = model.CategoryId,
                    Description = model.Description,
                    Date = model.Date,
                    Name = model.Name,
                    TransactionType = model.Type.HasValue ? (Models.TransactionType)model.Type : Models.TransactionType.Expense,
                    IsRecurring = model.IsRecurring,
                    RecurrenceInterval = model.RecurrenceInterval.HasValue ? (Models.RecurrenceInterval)model.RecurrenceInterval : Models.RecurrenceInterval.None,
                    UserId = userId
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Transaction added successfully!";
                return RedirectToAction("Transaction");
            }
            if(!ModelState.IsValid)
            {
                Console.WriteLine("some error occured");
            }

            var userIdForDropdown = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var categories = await _context.Categories
                .Where(c => c.UserId == null || c.UserId == userIdForDropdown)
                .ToListAsync();

            model.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Group = new SelectListGroup { Name = c.CategoryType.ToString() }
            }).ToList();

            return View("Transaction", model);
        }


        //for deleting transactions
        public async Task<IActionResult> Delete(int id)
        {
            var transaction = await _context.Transactions.SingleOrDefaultAsync(c => c.Id == id);
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction("Transaction");
        }

        public async Task<IActionResult> DeleteBudget(int id)
        {
            var budget = await _context.Budgets.SingleOrDefaultAsync(c => c.Id == id);
            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();
            return RedirectToAction("Budget");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTransaction(TransactionViewModel model)
        {
            var transaction = await _context.Transactions.FindAsync(model.Id);
            if (transaction == null)
            {
                return NotFound();
            }

            transaction.Date = model.Date;
            transaction.Name = model.Name;
            transaction.Amount = model.Amount;
            transaction.TransactionType = (Models.TransactionType)model.Type;
            transaction.Description = model.Description;
            transaction.CategoryId = model.CategoryId;
            transaction.IsRecurring = (bool)model.IsRecurring;
            transaction.RecurrenceInterval = (Models.RecurrenceInterval?)model.RecurrenceInterval;

            _context.Update(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction("Transaction");
        }

        [HttpPost]
        public async Task<IActionResult> EditBudget(BudgetViewModel model)
        {
            var budget = await _context.Budgets.FindAsync(model.id);

            if (budget == null)
            {
                return NotFound();
            }

            budget.Budget = model.Amount;
            budget.CategoryId = model.CategoryId.Value;

            _context.Update(budget);
            await _context.SaveChangesAsync();

            return RedirectToAction("Budget");
        }

        public async Task<IActionResult> CategoryDetails(int categoryId, string selectedMonth)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Filter transactions by category, user, and type
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.CategoryId == categoryId &&
                           t.UserId == userId &&
                           t.TransactionType == Models.TransactionType.Expense);

            DateTime? monthDate = null;

            // 2. Apply month filter if provided
            if (!string.IsNullOrEmpty(selectedMonth) &&
                DateTime.TryParseExact(selectedMonth, "MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                monthDate = parsedDate;
                query = query.Where(t => t.Date.Year == monthDate.Value.Year &&
                                       t.Date.Month == monthDate.Value.Month);
            }

            var transactions = await query.OrderBy(t => t.Date).ToListAsync();

            // 3. Get budget - filtered by month if applicable
            var budgetQuery = _context.Budgets
                .Where(b => b.UserId == userId && b.CategoryId == categoryId);

            if (monthDate.HasValue)
            {
                // Find either:
                // 1. A budget specifically for this month (BudgetforMonth matches exactly)
                // 2. Or a general budget (BudgetforMonth is null)
                budgetQuery = budgetQuery
                    .Where(b => b.BudgetforMonth == null ||
                               b.BudgetforMonth == selectedMonth)
                    .OrderByDescending(b => b.BudgetforMonth); // Prefer month-specific budgets
            }
            else
            {
                // When no month selected, only consider general budgets
                budgetQuery = budgetQuery.Where(b => b.BudgetforMonth == null);
            }

            var userBudget = await budgetQuery.FirstOrDefaultAsync();
            decimal budget = userBudget?.Budget ?? 0;

            // Rest of your view setup remains the same
            var view = new listViewModel
            {
                Transactions = transactions,
                CategoryName = transactions.FirstOrDefault()?.Category?.Name
            };

            decimal runningTotal = 0;
            var expenseData = new List<ChartPoint>();
            var budgetData = new List<ChartPoint>();

            int index = 1;
            foreach (var tx in transactions)
            {
                runningTotal += tx.Amount;
                expenseData.Add(new ChartPoint
                {
                    Index = index,
                    Amount = runningTotal,
                    Color = runningTotal > budget ? "red" : "#00bdae"
                });
                budgetData.Add(new ChartPoint
                {
                    Index = index,
                    Amount = budget,
                    Color = "blue"
                });
                index++;
            }

            if (!transactions.Any())
            {
                budgetData.Add(new ChartPoint
                {
                    Index = 1,
                    Amount = budget,
                    Color = "blue"
                });
            }

            CultureInfo nepalCulture = new CultureInfo("ne-NP");
            nepalCulture.NumberFormat.CurrencySymbol = "रु";
            ViewBag.DoughnutChart = transactions
                .Select(t => new
                {
                    name = t.Description,
                    amount = t.Amount,
                    formattedamount = t.Amount.ToString("C0", nepalCulture)
                })
                .ToList();

            ViewBag.ExpenseData = expenseData;
            ViewBag.BudgetData = budgetData;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.selectedid = categoryId;
            return View(view);
        }


        public async Task<IActionResult> Reports()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            int year = 2025; // Or DateTime.Now.Year for current year

            // Generate all months for the year as "MMMM yyyy"
            var allMonths = Enumerable.Range(1, 12)
                .Select(m => new DateTime(year, m, 1))
                .Select(d => d.ToString("MMMM yyyy"))
                .ToList();

            var monthlyExpenses = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Date.Year == year && t.TransactionType == Models.TransactionType.Expense)
                .GroupBy(t => t.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalExpense = g.Sum(t => t.Amount)
                })
                .ToListAsync();
            var monthlyincome = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Date.Year == year && t.TransactionType == Models.TransactionType.Income)
                .GroupBy(t => t.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Totalincome = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var monthlyBudget = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.BudgetforMonth.Contains(year.ToString()))
                .GroupBy(b => b.BudgetforMonth)
                .Select(g => new
                {
                    MonthLabel = g.Key, // Assume format "May 2025"
                    TotalBudget = g.Sum(b => b.Budget)
                })
                .ToListAsync();

            // Map budget data to month number for easier join (parse MonthLabel)
            var budgetByMonth = monthlyBudget.Select(b =>
            {
                var dt = DateTime.ParseExact(b.MonthLabel, "MMMM yyyy", null);
                return new { Month = dt.Month, b.TotalBudget };
            }).ToList();

            var combinedData = allMonths.Select(monthLabel =>
            {
                var dt = DateTime.ParseExact(monthLabel, "MMMM yyyy", null);
                var expense = monthlyExpenses.FirstOrDefault(e => e.Month == dt.Month)?.TotalExpense ?? 0;
                var budget = budgetByMonth.FirstOrDefault(b => b.Month == dt.Month)?.TotalBudget ?? 0;
                var income = monthlyincome.FirstOrDefault(i => i.Month == dt.Month)?.Totalincome ?? 0;
                return new ChartData
                {
                    xValue = monthLabel,
                    Expense = (double)expense,
                    Budget = (double)budget,
                    Income = (double)income 
                };
            })
            .ToList();

            ViewBag.ReportData = combinedData;

            return View();
        }

        public async Task<IActionResult> ExportToExcel(string? selectedMonth, int? selectedid)
        {
            string name= "unknown";
            try
            {
                // 1. Get the current user ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // 2. Fetch transactions from database
                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.UserId == userId && t.CategoryId == selectedid)
                    .OrderByDescending(t => t.Date)
                    .ToListAsync();

                // 3. Apply month filter if provided
                if (!string.IsNullOrEmpty(selectedMonth) && DateTime.TryParse(selectedMonth + "-01", out DateTime filterDate))
                {
                    transactions = transactions.Where(t => t.Date.Year == filterDate.Year && t.Date.Month == filterDate.Month).ToList();
                }

                // 4. Create CSV content
                var csv = new System.Text.StringBuilder();

                // 5. Add headers
                csv.AppendLine("Date,Description,Amount (NPR),Category,Type");

                // 6. Add data
                foreach (var transaction in transactions)
                {
                    var description = transaction.Description?.Replace("\"", "\"\"") ?? ""; // Escape quotes
                    var categoryName = transaction.Category?.Name?.Replace("\"", "\"\"") ?? "No Category";

                    csv.AppendLine($"\"{transaction.Date:yyyy-MM-dd}\"," +
                                  $"\"{description}\"," +
                                  $"{transaction.Amount}," +
                                  $"\"{categoryName}\"," +
                                  $"\"{transaction.TransactionType}\"");
                    name = categoryName;
                    
                }

                // 7. Add total at the end
                if (transactions.Any())
                {
                    csv.AppendLine($",,{transactions.Sum(t => t.Amount)},TOTAL,");
                }
               
                // 8. Convert to bytes and return file
                var fileName = $"ExpenseSummary_{name}{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                // Log error and return error message
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
    public class ChartPoint
    {
        public int Index { get; set; }
        public decimal Amount { get; set; }
        public string Color { get; set; }
    }

    public class ChartData
    {
        public string xValue { get; set; }
        public double Budget { get; set; }
        public double Expense { get; set; }

        public double Income { get; set; }
    }


}

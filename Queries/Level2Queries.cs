using Microsoft.EntityFrameworkCore;
using OnlineStoreLINQ.Data;
using OnlineStoreLINQ.Models;

namespace OnlineStoreLINQ.Queries;

public static class Level2Queries
{
    // Q7 (EN): Top 5 customers by total spending: name, order count, total spent, average order value.
    // Q7 (AR): أعلى 5 عملاء من حيث الصرف (Delivered بس): الاسم وعدد الأوردرات والإجمالي ومتوسط الأوردر.
    // Operators: Where, GroupBy, Distinct().Count(), Sum, OrderByDescending, Take
    public static async Task Question07(OnlineStoreContext db)
    {
        ConsoleHelper.Title(7, "Top 5 customers by spending");

        var result = await db.OrderItems
            .Where(i => i.Order.Status == OrderStatus.Delivered)
            .GroupBy(i => new { i.Order.CustomerId, i.Order.Customer.FullName })
            .Select(g => new
            {
                Name = g.Key.FullName,
                OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                TotalSpent = g.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(5)
            .ToListAsync();

        foreach (var r in result)
        {
            var avg = r.TotalSpent / r.OrderCount;   // average order value (tiny calculation on 5 rows)
            Console.WriteLine($"{r.Name} | orders = {r.OrderCount} | spent = {r.TotalSpent:F2} | avg order = {avg:F2}");
        }
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q8 (EN): Monthly report for 2025: Year, Month, OrderCount, Revenue, ordered by month.
    // Q8 (AR): تقرير شهري لسنة 2025: السنة والشهر وعدد الأوردرات والإيراد (Delivered بس).
    // Operators: Where, GroupBy (composite key), Distinct().Count(), Sum, OrderBy
    // BONUS (a): prints the generated SQL using ToQueryString().
    public static async Task Question08(OnlineStoreContext db)
    {
        ConsoleHelper.Title(8, "Monthly report for 2025");

        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2026, 1, 1);

        var query = db.OrderItems
            .Where(i => i.Order.Status == OrderStatus.Delivered
                     && i.Order.OrderDate >= startDate
                     && i.Order.OrderDate < endDate)
            .GroupBy(i => new { i.Order.OrderDate.Year, i.Order.OrderDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
            })
            .OrderBy(x => x.Month);

        // BONUS (a): ToQueryString() must be called BEFORE ToListAsync()
        Console.WriteLine("--- Generated SQL (Bonus a) ---");
        Console.WriteLine(query.ToQueryString());
        Console.WriteLine("-------------------------------");

        var result = await query.ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.Year}-{r.Month:00} | orders = {r.OrderCount} | revenue = {r.Revenue:F2}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q9 (EN): Revenue per category, ordered by revenue descending.
    // Q9 (AR): الإيراد لكل Category مرتب من الأكبر للأصغر.
    // Operators: Where, GroupBy, Sum, OrderByDescending
    public static async Task Question09(OnlineStoreContext db)
    {
        ConsoleHelper.Title(9, "Revenue per category");

        var result = await db.OrderItems
            .Where(i => i.Order.Status == OrderStatus.Delivered)
            .GroupBy(i => i.Product.Category.Name)
            .Select(g => new
            {
                Category = g.Key,
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.Category} | {r.Revenue:F2}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q10 (EN): Count customers who never placed an order, grouped by city, ordered by count descending.
    // Q10 (AR): عدد العملاء اللي ماعملوش أي أوردر، متجمعين بالمدينة ومرتبين تنازلي.
    // Operators: Where, Any (negated), GroupBy, Count, OrderByDescending
    public static async Task Question10(OnlineStoreContext db)
    {
        ConsoleHelper.Title(10, "Customers without orders by city");

        var result = await db.Customers
            .Where(c => !c.Orders.Any())
            .GroupBy(c => c.City)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.City} | {r.Count}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q11 (EN): Products with at least 10 reviews and average rating >= 4.5.
    // Q11 (AR): المنتجات اللي ليها 10 ريفيوهات على الأقل ومتوسط تقييم 4.5 أو أكتر.
    // Operators: Where, Count, Average, Select
    public static async Task Question11(OnlineStoreContext db)
    {
        ConsoleHelper.Title(11, "Highly rated products (>= 10 reviews, avg >= 4.5)");

        var result = await db.Products
            .Where(p => p.Reviews.Count >= 10 && p.Reviews.Average(r => r.Rating) >= 4.5)
            .Select(p => new
            {
                p.Name,
                Category = p.Category.Name,
                ReviewCount = p.Reviews.Count,
                AvgRating = p.Reviews.Average(r => r.Rating)
            })
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.Name} ({r.Category}) | reviews = {r.ReviewCount} | avg = {r.AvgRating:F2}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q12 (EN): For each category return the top 2 most expensive products.
    // Q12 (AR): لكل Category أغلى منتجين.
    // Operators: Select (nested), OrderByDescending, Take
    public static async Task Question12(OnlineStoreContext db)
    {
        ConsoleHelper.Title(12, "Top 2 most expensive products per category");

        var result = await db.Categories
            .Select(c => new
            {
                Category = c.Name,
                Top2 = c.Products
                    .OrderByDescending(p => p.Price)
                    .Take(2)
                    .Select(p => new { p.Name, p.Price })
                    .ToList()
            })
            .ToListAsync();

        foreach (var c in result)
        {
            Console.WriteLine(c.Category);
            foreach (var p in c.Top2)
                Console.WriteLine($"   {p.Name} | {p.Price:F2}");
        }
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q13 (EN): Products whose stock is lower than the total quantity in Pending orders.
    // Q13 (AR): المنتجات اللي الـ Stock بتاعها أقل من إجمالي الكمية في أوردرات الـ Pending.
    // Operators: Select, Where (inside Sum), Sum, Where
    public static async Task Question13(OnlineStoreContext db)
    {
        ConsoleHelper.Title(13, "Stock lower than pending quantity");

        var result = await db.Products
            .Select(p => new
            {
                p.Name,
                p.Stock,
                PendingQty = p.OrderItems
                    .Where(i => i.Order.Status == OrderStatus.Pending)
                    .Sum(i => i.Quantity)
            })
            .Where(x => x.Stock < x.PendingQty)
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.Name} | stock = {r.Stock} | pending qty = {r.PendingQty}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q14 (EN): Customers whose total spending is above the average spending of all customers.
    // Q14 (AR): العملاء اللي إجمالي صرفهم أعلى من متوسط صرف كل العملاء.
    // Operators: Select, SelectMany, Sum, Average, Where
    // Note: customers with zero spending are included in the average ("all customers").
    public static async Task Question14(OnlineStoreContext db)
    {
        ConsoleHelper.Title(14, "Customers spending above the average");

        var spending = db.Customers.Select(c => new
        {
            c.FullName,
            Spent = c.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.Items)
                .Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
        });

        var average = await spending.AverageAsync(x => x.Spent);
        Console.WriteLine($"Average spending of all customers = {average:F2}");

        var result = await spending
            .Where(x => x.Spent > average)
            .OrderByDescending(x => x.Spent)
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.FullName} | spent = {r.Spent:F2}");
        ConsoleHelper.IfEmpty(result.Count);
    }
}

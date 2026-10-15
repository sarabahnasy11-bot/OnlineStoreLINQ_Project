using Microsoft.EntityFrameworkCore;
using OnlineStoreLINQ.Data;
using OnlineStoreLINQ.Models;

namespace OnlineStoreLINQ.Queries;

public static class Level1Queries
{
    // Q1 (EN): For every delivered order return OrderId, CustomerName and Total, ordered by Total descending.
    // Q1 (AR): لكل أوردر Delivered رجّع رقمه واسم العميل والإجمالي، مرتبين من الأكبر للأصغر.
    // Operators: Where, Select, Sum, OrderByDescending
    public static async Task Question01(OnlineStoreContext db)
    {
        ConsoleHelper.Title(1, "Delivered orders with totals");

        var result = await db.Orders
            .Where(o => o.Status == OrderStatus.Delivered)
            .Select(o => new
            {
                OrderId = o.Id,
                CustomerName = o.Customer.FullName,
                Total = o.Items.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"Order #{r.OrderId} | {r.CustomerName} | Total = {r.Total:F2}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q2 (EN): Count orders per status with the percentage of the whole.
    // Q2 (AR): عدد الأوردرات لكل Status مع النسبة المئوية من الإجمالي (كل الـ 4 Status حتى لو عددهم صفر).
    // Operators: GroupBy, Select (SQL), then Enum.GetValues so statuses with 0 orders still appear
    public static async Task Question02(OnlineStoreContext db)
    {
        ConsoleHelper.Title(2, "Orders per status with percentage");

        // SQL returns at most 4 small rows. A status with no orders does not appear here.
        var counts = await db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalOrders = counts.Sum(c => c.Count);

        // Loop over ALL enum values so Pending/Shipped/Delivered/Cancelled always show up.
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            var count = counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;
            var percent = totalOrders == 0 ? 0.0 : 100.0 * count / totalOrders;
            Console.WriteLine($"{status,-10} | {count} orders | {percent:F1}%");
        }
    }

    // Q3 (EN): Products whose price is above the average price of their own category.
    // Q3 (AR): المنتجات اللي سعرها أعلى من متوسط سعر نفس الـ Category بتاعها.
    // Operators: Where, Average (correlated subquery)
    public static async Task Question03(OnlineStoreContext db)
    {
        ConsoleHelper.Title(3, "Products above their category's average price");

        var result = await db.Products
            .Where(p => p.Price > db.Products
                .Where(other => other.CategoryId == p.CategoryId)
                .Average(other => other.Price))
            .Select(p => new { p.Name, p.Price, Category = p.Category.Name })
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.Name} ({r.Category}) | {r.Price:F2}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q4 (EN): Products that were never ordered.
    // Q4 (AR): المنتجات اللي عمرها ما اتطلبت.
    // Operators: Where, Any (negated)
    public static async Task Question04(OnlineStoreContext db)
    {
        ConsoleHelper.Title(4, "Products never ordered");

        var result = await db.Products
            .Where(p => !p.OrderItems.Any())
            .Select(p => p.Name)
            .ToListAsync();

        foreach (var name in result)
            Console.WriteLine(name);
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q5 (EN): Customers registered in the last 30 days who have at least one order.
    // Q5 (AR): العملاء اللي سجلوا في آخر 30 يوم وعندهم أوردر واحد على الأقل.
    // Operators: Where, Any
    public static async Task Question05(OnlineStoreContext db)
    {
        ConsoleHelper.Title(5, "Recent customers with at least one order");

        var since = DateTime.UtcNow.AddDays(-30);   // calculated OUTSIDE the query

        var result = await db.Customers
            .Where(c => c.RegisteredAt >= since && c.Orders.Any())
            .Select(c => new { c.FullName, c.RegisteredAt })
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.FullName} | registered {r.RegisteredAt:yyyy-MM-dd}");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q6 (EN): Customers who cancelled more than 2 orders, with the number of cancelled orders.
    // Q6 (AR): العملاء اللي لغوا أكتر من 2 أوردر مع عدد الأوردرات الملغية.
    // Operators: Select, Count(predicate), Where
    public static async Task Question06(OnlineStoreContext db)
    {
        ConsoleHelper.Title(6, "Customers with more than 2 cancelled orders");

        var result = await db.Customers
            .Select(c => new
            {
                c.FullName,
                CancelledCount = c.Orders.Count(o => o.Status == OrderStatus.Cancelled)
            })
            .Where(x => x.CancelledCount > 2)
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.FullName} | cancelled = {r.CancelledCount}");
        ConsoleHelper.IfEmpty(result.Count);
    }
}

using Microsoft.EntityFrameworkCore;
using OnlineStoreLINQ.Data;
using OnlineStoreLINQ.Models;

namespace OnlineStoreLINQ.Queries;

public static class Level3Queries
{
    // Q15 (EN): Best-selling product (by quantity) per category. Categories with no sales must still appear.
    // Q15 (AR): أكتر منتج مبيعاً (بالكمية) في كل Category، والـ Categories من غير مبيعات لازم تظهر.
    // Operators: Select (start from Categories => like a LEFT JOIN), Sum, Where, OrderByDescending, FirstOrDefault
    // "Sales" = Delivered orders only.
    public static async Task Question15(OnlineStoreContext db)
    {
        ConsoleHelper.Title(15, "Best-selling product per category");

        var result = await db.Categories
            .Select(c => new
            {
                Category = c.Name,
                Best = c.Products
                    .Select(p => new
                    {
                        p.Name,
                        Qty = p.OrderItems
                            .Where(i => i.Order.Status == OrderStatus.Delivered)
                            .Sum(i => i.Quantity)
                    })
                    .Where(x => x.Qty > 0)
                    .OrderByDescending(x => x.Qty)
                    .ThenBy(x => x.Name)          // tie-breaker: same result every run
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var r in result)
        {
            var text = r.Best == null ? "(no sales)" : $"{r.Best.Name} (qty {r.Best.Qty})";
            Console.WriteLine($"{r.Category} | {text}");
        }
    }

    // Q16 (EN): Customers who bought every product in the category "Laptops".
    // Q16 (AR): العملاء اللي اشتروا كل المنتجات في Category اسمها "Laptops".
    // Operators: Contains, Distinct, Count (compare with number of laptops)
    // "Bought" = in a Delivered order.
    public static async Task Question16(OnlineStoreContext db)
    {
        ConsoleHelper.Title(16, "Customers who bought every Laptop");

        var laptopIds = await db.Products
            .Where(p => p.Category.Name == "Laptops")
            .Select(p => p.Id)
            .ToListAsync();

        if (laptopIds.Count == 0)
        {
            Console.WriteLine("(no products in category 'Laptops')");
            return;
        }

        var result = await db.Customers
            .Where(c => c.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SelectMany(o => o.Items)
                .Where(i => laptopIds.Contains(i.ProductId))
                .Select(i => i.ProductId)
                .Distinct()
                .Count() == laptopIds.Count)
            .Select(c => c.FullName)
            .ToListAsync();

        foreach (var name in result)
            Console.WriteLine(name);
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q17 (EN): Reviews written by a customer for a product they never bought.
    // Q17 (AR): ريفيوهات اتكتبت من عميل على منتج عمره ما اشتراه.
    // Operators: Where, Any (negated, correlated)
    // "Bought" = in a Delivered order.
    public static async Task Question17(OnlineStoreContext db)
    {
        ConsoleHelper.Title(17, "Reviews for products the customer never bought");

        var result = await db.Reviews
            .Where(r => !db.OrderItems.Any(i =>
                i.ProductId == r.ProductId
                && i.Order.CustomerId == r.CustomerId
                && i.Order.Status == OrderStatus.Delivered))
            .Select(r => new
            {
                Customer = r.Customer.FullName,
                Product = r.Product.Name,
                r.Rating
            })
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.Customer} reviewed {r.Product} (rating {r.Rating})");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q18 (EN): All customers with their order count, including zero (left join with GroupJoin).
    // Q18 (AR): كل العملاء مع عدد أوردراتهم حتى اللي عندهم صفر (left join باستخدام GroupJoin).
    // Operators: GroupJoin, Count
    public static async Task Question18(OnlineStoreContext db)
    {
        ConsoleHelper.Title(18, "All customers with order count (GroupJoin)");

        var result = await db.Customers
            .GroupJoin(
                db.Orders,                       // the other table
                customer => customer.Id,         // key from Customers
                order => order.CustomerId,       // key from Orders
                (customer, orders) => new        // customer + ALL its orders (maybe empty)
                {
                    customer.FullName,
                    OrderCount = orders.Count()
                })
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.FullName} | orders = {r.OrderCount}");
    }

    // Q19 (EN): Top 5 product pairs that appear together in the same order most often.
    // Q19 (AR): أعلى 5 أزواج منتجات بتتشترى مع بعض في نفس الأوردر.
    // Operators: join (self-join), where (A < B to avoid duplicates and (A,A) pairs), GroupBy, Distinct().Count()
    public static async Task Question19(OnlineStoreContext db)
    {
        ConsoleHelper.Title(19, "Top 5 product pairs bought together");

        // Step 1: every pair of different products inside the same order
        var pairs =
            from a in db.OrderItems
            join b in db.OrderItems on a.OrderId equals b.OrderId
            where a.ProductId < b.ProductId
            select new
            {
                a.OrderId,
                FirstId = a.ProductId,
                FirstName = a.Product.Name,
                SecondId = b.ProductId,
                SecondName = b.Product.Name
            };

        // Step 2: count in how many different orders each pair appears
        var result = await pairs
            .GroupBy(p => new { p.FirstId, p.FirstName, p.SecondId, p.SecondName })
            .Select(g => new
            {
                g.Key.FirstName,
                g.Key.SecondName,
                Orders = g.Select(p => p.OrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.Orders)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.SecondName)
            .Take(5)
            .ToListAsync();

        foreach (var r in result)
            Console.WriteLine($"{r.FirstName} + {r.SecondName} | together in {r.Orders} order(s)");
        ConsoleHelper.IfEmpty(result.Count);
    }

    // Q20 (EN): For each customer: name + most recent order with each item's product name and quantity, in ONE query.
    // Q20 (AR): لكل عميل: الاسم + آخر أوردر بالـ Items (اسم المنتج والكمية) في query واحدة.
    // Operators: Select (nested projection), OrderByDescending, FirstOrDefault
    public static async Task Question20(OnlineStoreContext db)
    {
        ConsoleHelper.Title(20, "Each customer's latest order with items (1 query)");

        var result = await db.Customers
            .Select(c => new
            {
                c.FullName,
                LastOrder = c.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderDate,
                        Items = o.Items
                            .Select(i => new { ProductName = i.Product.Name, i.Quantity })
                            .ToList()
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var c in result)
        {
            if (c.LastOrder == null)
            {
                Console.WriteLine($"{c.FullName} | no orders");
                continue;
            }
            Console.WriteLine($"{c.FullName} | order #{c.LastOrder.Id} on {c.LastOrder.OrderDate:yyyy-MM-dd}");
            foreach (var item in c.LastOrder.Items)
                Console.WriteLine($"      {item.ProductName} x {item.Quantity}");
        }
    }

    // Q20 fallback (EN): same result using a FILTERED INCLUDE. Still ONE SQL query, still no N+1.
    // Use this (menu option 20B) only if Question20 throws a translation error on your EF Core version.
    // Q20 fallback (AR): نفس النتيجة بـ Include مفلتر. برضو query واحدة وبدون N+1.
    public static async Task Question20_FilteredIncludeVersion(OnlineStoreContext db)
    {
        ConsoleHelper.Title("Question 20 (filtered Include version)");

        var customers = await db.Customers
            .AsNoTracking()
            .Include(c => c.Orders.OrderByDescending(o => o.OrderDate).Take(1))
                .ThenInclude(o => o.Items)
                    .ThenInclude(i => i.Product)
            .ToListAsync();

        foreach (var c in customers)
        {
            var last = c.Orders.FirstOrDefault();
            if (last == null)
            {
                Console.WriteLine($"{c.FullName} | no orders");
                continue;
            }
            Console.WriteLine($"{c.FullName} | order #{last.Id} on {last.OrderDate:yyyy-MM-dd}");
            foreach (var item in last.Items)
                Console.WriteLine($"      {item.Product.Name} x {item.Quantity}");
        }
    }

    // BONUS (b): the N+1 problem. This is what question 20 looks like if written with a foreach loop.
    // The loop sends 1 query for customers + 2 queries PER customer.
    public static async Task Question20_NPlusOneDemo(OnlineStoreContext db)
    {
        ConsoleHelper.Title("Bonus (b): N+1 problem demo for Question 20");

        var customers = await db.Customers.ToListAsync();      // 1 query
        int queryCount = 1;

        foreach (var customer in customers)                    // N customers...
        {
            var lastOrder = await db.Orders                    // ...+1 query for EACH customer
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();
            queryCount++;

            if (lastOrder == null) continue;

            var items = await db.OrderItems                    // ...+1 more query for EACH customer
                .Where(i => i.OrderId == lastOrder.Id)
                .Include(i => i.Product)
                .ToListAsync();
            queryCount++;
        }

        Console.WriteLine($"Customers: {customers.Count}");
        Console.WriteLine($"Queries sent by the foreach version: {queryCount}");
        Console.WriteLine("Queries sent by Question20 (projection): 1");
        Console.WriteLine("With 1000 customers the loop would send about 2001 queries - this is the N+1 problem.");
    }

    // Q21 (EN): For each customer, their favorite category (highest total quantity bought).
    // Q21 (AR): لكل عميل الـ Category المفضلة (اللي اشترى منها أكبر كمية).
    // Customers with NO purchases are still listed and shown as "(no purchases)".
    // Operators: GroupBy + Sum (in SQL), then ToDictionary + OrderByDescending + First (in memory on a small list)
    public static async Task Question21(OnlineStoreContext db)
    {
        ConsoleHelper.Title(21, "Favorite category per customer");

        // Step 1 (SQL): quantity per (customer, category), Delivered orders only
        var totals = await db.OrderItems
            .Where(i => i.Order.Status == OrderStatus.Delivered)
            .GroupBy(i => new { i.Order.CustomerId, Category = i.Product.Category.Name })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Category,
                Qty = g.Sum(i => i.Quantity)
            })
            .ToListAsync();

        // Step 2 (memory, small list): best category for each customer that bought something
        var favoriteByCustomer = totals
            .GroupBy(x => x.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.Qty).ThenBy(x => x.Category).First());

        // Step 3: ALL customers (so customers without purchases appear too)
        var customers = await db.Customers
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            .Select(c => new { c.Id, c.FullName })
            .ToListAsync();

        foreach (var c in customers)
        {
            if (favoriteByCustomer.TryGetValue(c.Id, out var favorite))
                Console.WriteLine($"{c.FullName} | {favorite.Category} (qty {favorite.Qty})");
            else
                Console.WriteLine($"{c.FullName} | (no purchases)");
        }
    }

    // Q22 (EN): Rank products by revenue (1, 2, 3 ...) and return Rank, Product, Revenue.
    // Q22 (AR): ترتيب المنتجات حسب الإيراد (1، 2، 3 ...). بنجمّع بـ ProductId واسم المنتج عشان منتجين بنفس الاسم مايتدمجوش.
    // Operators: GroupBy (ProductId + Name) + Sum + OrderByDescending (in SQL), then Select((x, i) => ...) AFTER ToListAsync (in memory)
    // Only products with Delivered sales are ranked. Ranks are unique (1,2,3...); ties are ordered by Name then Id.
    public static async Task Question22(OnlineStoreContext db)
    {
        ConsoleHelper.Title(22, "Products ranked by revenue");

        // Step 1: SQL does the heavy work (group, sum, sort). One row per PRODUCT (not per name).
        var rows = await db.OrderItems
            .Where(i => i.Order.Status == OrderStatus.Delivered)
            .GroupBy(i => new { i.ProductId, i.Product.Name })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                Product = g.Key.Name,
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
            })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.Product)          // tie-breakers so the order is always the same
            .ThenBy(x => x.ProductId)
            .ToListAsync();                  // <-- from here we are in memory

        // Step 2: the index overload works here because it runs in C#, not SQL.
        var ranked = rows.Select((x, index) => new
        {
            Rank = index + 1,
            x.ProductId,
            x.Product,
            x.Revenue
        });

        foreach (var r in ranked)
            Console.WriteLine($"#{r.Rank} | {r.Product} (Id {r.ProductId}) | {r.Revenue:F2}");
        ConsoleHelper.IfEmpty(rows.Count);
    }

    // BONUS (c): why Select((x, i) => ...) cannot be translated to SQL, and the fix.
    public static void Question22_WhyIndexSelectFails()
    {
        ConsoleHelper.Title("Bonus (c): Select with index in Question 22");
        Console.WriteLine("Why it fails:");
        Console.WriteLine("  A SQL table/result is an UNORDERED SET of rows. A row has no built-in position,");
        Console.WriteLine("  so EF Core has no SQL to translate the 'index' parameter of Select((x, i) => ...).");
        Console.WriteLine("  Calling it on an IQueryable throws an InvalidOperationException at run time.");
        Console.WriteLine("The fix:");
        Console.WriteLine("  1) Let SQL group/sum/sort, call ToListAsync(), then use Select((x, i) => ...) in memory.");
        Console.WriteLine("     (This is what Question22 does. Safe because the list has one row per product.)");
        Console.WriteLine("  2) Or use the SQL window function ROW_NUMBER()/RANK() OVER (ORDER BY Revenue DESC)");
        Console.WriteLine("     through raw SQL (FromSql).");
    }
}

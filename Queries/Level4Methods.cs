using Microsoft.EntityFrameworkCore;
using OnlineStoreLINQ.Data;
using OnlineStoreLINQ.Filters;
using OnlineStoreLINQ.Models;

namespace OnlineStoreLINQ.Queries;

public static class Level4Methods
{
    // Q23 (EN): SearchProducts(ProductFilter f): optional filters, sorting (name/price/rating), paging + total count.
    // Q23 (AR): بحث عن المنتجات بفلاتر اختيارية وترتيب وصفحات مع العدد الكلي.
    // Operators: Where (added only when the filter exists), CountAsync, Select, OrderBy, ThenBy, Skip, Take
    public static async Task<PagedResult<ProductDto>> SearchProducts(OnlineStoreContext db, ProductFilter f)
    {
        // Start with ALL products. Nothing is sent to SQL until we call CountAsync/ToListAsync.
        IQueryable<Product> query = db.Products.AsNoTracking();

        // Add a Where ONLY if that filter was provided
        if (!string.IsNullOrWhiteSpace(f.Text))
            query = query.Where(p => p.Name.Contains(f.Text));

        if (f.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == f.CategoryId.Value);

        if (f.MinPrice.HasValue)
            query = query.Where(p => p.Price >= f.MinPrice.Value);

        if (f.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= f.MaxPrice.Value);

        if (f.InStockOnly)
            query = query.Where(p => p.Stock > 0);

        // Total count of ALL matches (BEFORE paging)
        var totalCount = await query.CountAsync();

        // Project to the DTO (the average rating is calculated in SQL)
        var projected = query.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Stock = p.Stock,
            Category = p.Category.Name,
            AvgRating = p.Reviews.Average(r => (double?)r.Rating)
        });

        // Sorting
        IOrderedQueryable<ProductDto> sorted;
        switch (f.SortBy?.ToLowerInvariant())
        {
            case "price":
                sorted = f.Descending ? projected.OrderByDescending(x => x.Price) : projected.OrderBy(x => x.Price);
                break;
            case "rating":
                sorted = f.Descending ? projected.OrderByDescending(x => x.AvgRating) : projected.OrderBy(x => x.AvgRating);
                break;
            default: // "name"
                sorted = f.Descending ? projected.OrderByDescending(x => x.Name) : projected.OrderBy(x => x.Name);
                break;
        }

        // Paging (safe values)
        var page = f.Page < 1 ? 1 : f.Page;
        var pageSize = f.PageSize < 1 ? 10 : f.PageSize;

        var items = await sorted
            .ThenBy(x => x.Id)                 // makes paging stable when values are equal
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProductDto> { Items = items, TotalCount = totalCount };
    }

    // Demo for Q23: calls SearchProducts with a few different filters.
    public static async Task Question23(OnlineStoreContext db)
    {
        ConsoleHelper.Title(23, "SearchProducts");

        var filter1 = new ProductFilter
        {
            Text = "o",
            MinPrice = 20m,
            InStockOnly = true,
            SortBy = "price",
            Descending = true,
            Page = 1,
            PageSize = 4
        };
        Console.WriteLine("Filter 1: name contains 'o', price >= 20, in stock, sort by price desc, page 1 (size 4)");
        PrintPage(await SearchProducts(db, filter1));

        var filter2 = new ProductFilter { SortBy = "rating", Descending = true, Page = 1, PageSize = 3 };
        Console.WriteLine("\nFilter 2: no filters, sort by rating desc, page 1 (size 3)");
        PrintPage(await SearchProducts(db, filter2));
    }

    private static void PrintPage(PagedResult<ProductDto> page)
    {
        Console.WriteLine($"Total matching products: {page.TotalCount}");
        foreach (var p in page.Items)
        {
            var rating = p.AvgRating.HasValue ? p.AvgRating.Value.ToString("F2") : "n/a";
            Console.WriteLine($"   {p.Name} | {p.Category} | {p.Price:F2} | stock {p.Stock} | rating {rating}");
        }
    }

    // Q24 (EN): Increase price by 10% for "Accessories" products with stock below 5, WITHOUT loading them into memory.
    // Q24 (AR): زوّد السعر 10% لمنتجات "Accessories" اللي الـ Stock بتاعها أقل من 5 من غير ما نحمّلها في الـ memory.
    // Operators: Where + Any (subquery on Categories), ExecuteUpdateAsync + SetProperty = ONE UPDATE statement in SQL
    // WARNING: this changes the data (running it twice raises the price twice). Use option R to reset.
    public static async Task Question24(OnlineStoreContext db)
    {
        ConsoleHelper.Title(24, "Increase Accessories prices by 10% (stock < 5)");

        var affected = await db.Products
            .Where(p => p.Stock < 5
                     && db.Categories.Any(c => c.Id == p.CategoryId && c.Name == "Accessories"))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Price, p => p.Price * 1.10m));

        Console.WriteLine($"Rows updated: {affected}");
    }

    // OPTIONAL display helper for Q24 (NOT part of the update). It reads the products only to print them.
    public static async Task ShowAccessoryPrices(OnlineStoreContext db, string label)
    {
        var accessories = await db.Products
            .AsNoTracking()
            .Where(p => p.Category.Name == "Accessories")
            .OrderBy(p => p.Name)
            .Select(p => new { p.Name, p.Price, p.Stock })
            .ToListAsync();

        Console.WriteLine($"--- Accessories ({label}) ---");
        foreach (var p in accessories)
            Console.WriteLine($"   {p.Name} | price {p.Price:F2} | stock {p.Stock}");
    }

    // Q25 (EN): Delete all cancelled orders older than 1 year WITHOUT loading them into memory.
    // Q25 (AR): امسح كل الأوردرات الملغية الأقدم من سنة من غير ما نحمّلها في الـ memory.
    // Operators: Where, Any, ExecuteDeleteAsync (one DELETE statement in SQL), transaction
    // WARNING: this deletes data. Use option R in the menu to reset the database.
    public static async Task Question25(OnlineStoreContext db)
    {
        ConsoleHelper.Title(25, "Delete cancelled orders older than 1 year");

        var cutoff = DateTime.UtcNow.AddYears(-1);

        var countBefore = await db.Orders
            .CountAsync(o => o.Status == OrderStatus.Cancelled && o.OrderDate < cutoff);
        Console.WriteLine($"Orders that match: {countBefore}");

        // Transaction: either both deletes succeed, or none.
        await using var transaction = await db.Database.BeginTransactionAsync();

        // 1) delete the items of those orders first (in case cascade delete is not configured)
        await db.OrderItems
            .Where(i => db.Orders.Any(o => o.Id == i.OrderId
                                        && o.Status == OrderStatus.Cancelled
                                        && o.OrderDate < cutoff))
            .ExecuteDeleteAsync();

        // 2) delete the orders
        var deletedOrders = await db.Orders
            .Where(o => o.Status == OrderStatus.Cancelled && o.OrderDate < cutoff)
            .ExecuteDeleteAsync();

        await transaction.CommitAsync();

        Console.WriteLine($"Orders deleted: {deletedOrders}");
    }
}

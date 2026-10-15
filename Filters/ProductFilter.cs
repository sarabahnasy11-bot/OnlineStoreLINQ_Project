namespace OnlineStoreLINQ.Filters;

// Input of SearchProducts (Q23). Every filter is optional.
public class ProductFilter
{
    public string? Text { get; set; }          // part of the product name
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; }

    public string SortBy { get; set; } = "name";   // "name" | "price" | "rating"
    public bool Descending { get; set; }

    public int Page { get; set; } = 1;             // starts from 1
    public int PageSize { get; set; } = 10;
}

// One row of the result.
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public double? AvgRating { get; set; }     // null if the product has no reviews
}

// A page of results + the total number of matching rows.
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

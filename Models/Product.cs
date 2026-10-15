namespace OnlineStoreLINQ.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public List<OrderItem> OrderItems { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
}

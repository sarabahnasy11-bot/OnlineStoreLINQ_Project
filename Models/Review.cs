namespace OnlineStoreLINQ.Models;

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int Rating { get; set; } // 1..5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

namespace OnlineStoreLINQ.Models;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }

    public List<Order> Orders { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
}

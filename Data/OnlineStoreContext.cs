using Microsoft.EntityFrameworkCore;
using OnlineStoreLINQ.Models;

namespace OnlineStoreLINQ.Data;

public class OnlineStoreContext : DbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // The connection string comes from ConnectionHelper (environment variable or appsettings.json)
        optionsBuilder.UseSqlServer(ConnectionHelper.Get());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Decimal precision (money and discount)
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(i => i.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(i => i.Discount).HasPrecision(5, 2);

        // Restrict = you cannot delete a Product/Customer that still has items/reviews.
        // This also avoids SQL Server's "multiple cascade paths" error.
        // Order -> OrderItem and Customer -> Order keep the default cascade delete.
        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Product).WithMany(p => p.OrderItems)
            .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Product).WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer).WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

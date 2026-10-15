using Microsoft.EntityFrameworkCore;
using OnlineStoreLINQ.Models;

namespace OnlineStoreLINQ.Data;

// Sample data designed so that every question returns something interesting.
public static class SeedData
{
    public static async Task SeedAsync(OnlineStoreContext db)
    {
        if (await db.Categories.AnyAsync())
        {
            Console.WriteLine("Database already contains data - seed skipped.");
            return;
        }

        var now = DateTime.UtcNow;

        // ---------- Categories ----------
        var laptops = new Category { Name = "Laptops" };
        var accessories = new Category { Name = "Accessories" };
        var phones = new Category { Name = "Phones" };
        var monitors = new Category { Name = "Monitors" };
        var books = new Category { Name = "Books" };   // never sold (for Q15)

        // ---------- Products ----------
        var proLaptop = new Product { Name = "Pro Laptop 15", Price = 1500m, Stock = 20, Category = laptops };
        var airLaptop = new Product { Name = "Air Laptop 13", Price = 1200m, Stock = 15, Category = laptops };
        var gamingLaptop = new Product { Name = "Gaming Laptop X", Price = 2000m, Stock = 3, Category = laptops };
        var mouse = new Product { Name = "Wireless Mouse", Price = 25m, Stock = 3, Category = accessories };
        var keyboard = new Product { Name = "Mechanical Keyboard", Price = 80m, Stock = 4, Category = accessories };
        var hub = new Product { Name = "USB-C Hub", Price = 40m, Stock = 50, Category = accessories };
        var sleeve = new Product { Name = "Laptop Sleeve", Price = 20m, Stock = 2, Category = accessories };
        var phoneAlpha = new Product { Name = "Phone Alpha", Price = 800m, Stock = 30, Category = phones };
        var phoneBeta = new Product { Name = "Phone Beta", Price = 600m, Stock = 8, Category = phones };
        var monitor27 = new Product { Name = "Monitor 27", Price = 300m, Stock = 10, Category = monitors };
        var monitor4k = new Product { Name = "Monitor 32 4K", Price = 550m, Stock = 6, Category = monitors };
        // Two DIFFERENT products with the SAME name (Q22 must not merge them)
        var cableAccessory = new Product { Name = "Charging Cable", Price = 10m, Stock = 100, Category = accessories };
        var cablePhone = new Product { Name = "Charging Cable", Price = 15m, Stock = 80, Category = phones };
        var linqBook = new Product { Name = "LINQ in Action", Price = 45m, Stock = 100, Category = books };   // never ordered (Q4)
        var csharpBook = new Product { Name = "C# Basics", Price = 30m, Stock = 100, Category = books };      // never ordered (Q4)

        var products = new List<Product>
        {
            proLaptop, airLaptop, gamingLaptop, mouse, keyboard, hub, sleeve,
            phoneAlpha, phoneBeta, monitor27, monitor4k, cableAccessory, cablePhone, linqBook, csharpBook
        };

        // ---------- Customers ----------
        var ahmed = new Customer { FullName = "Ahmed Hassan", Email = "ahmed@example.com", City = "Cairo", RegisteredAt = new DateTime(2024, 3, 10) };
        var sara = new Customer { FullName = "Sara Ali", Email = "sara@example.com", City = "Alexandria", RegisteredAt = new DateTime(2024, 6, 1) };
        var omar = new Customer { FullName = "Omar Khaled", Email = "omar@example.com", City = "Cairo", RegisteredAt = new DateTime(2024, 9, 15) };
        var mona = new Customer { FullName = "Mona Said", Email = "mona@example.com", City = "Giza", RegisteredAt = new DateTime(2025, 1, 20) };
        var youssef = new Customer { FullName = "Youssef Adel", Email = "youssef@example.com", City = "Alexandria", RegisteredAt = new DateTime(2025, 2, 11) };
        var nour = new Customer { FullName = "Nour Hany", Email = "nour@example.com", City = "Cairo", RegisteredAt = new DateTime(2025, 5, 5) };
        var laila = new Customer { FullName = "Laila Fathy", Email = "laila@example.com", City = "Giza", RegisteredAt = new DateTime(2025, 8, 8) };
        var karim = new Customer { FullName = "Karim Samir", Email = "karim@example.com", City = "Alexandria", RegisteredAt = now.AddDays(-10) };   // recent + has orders (Q5)
        var hana = new Customer { FullName = "Hana Magdy", Email = "hana@example.com", City = "Cairo", RegisteredAt = now.AddDays(-5) };          // recent, no orders
        var tarek = new Customer { FullName = "Tarek Nabil", Email = "tarek@example.com", City = "Giza", RegisteredAt = new DateTime(2025, 11, 30) };   // no orders
        var dina = new Customer { FullName = "Dina Ezzat", Email = "dina@example.com", City = "Alexandria", RegisteredAt = new DateTime(2026, 1, 1) };  // no orders
        var salma = new Customer { FullName = "Salma Wael", Email = "salma@example.com", City = "Cairo", RegisteredAt = new DateTime(2025, 10, 1) };     // has ONLY a cancelled order (no purchases)

        // NOTE: the first 10 customers are used by the review loops below.
        var customers = new List<Customer> { ahmed, sara, omar, mona, youssef, nour, laila, karim, hana, tarek, dina, salma };

        // ---------- Orders ----------
        var orders = new List<Order>();

        void AddOrder(Customer customer, DateTime date, OrderStatus status,
                      params (Product Product, int Qty, decimal Discount)[] items)
        {
            var order = new Order { Customer = customer, OrderDate = date, Status = status };
            foreach (var item in items)
            {
                order.Items.Add(new OrderItem
                {
                    Product = item.Product,
                    Quantity = item.Qty,
                    UnitPrice = item.Product.Price,
                    Discount = item.Discount
                });
            }
            orders.Add(order);
        }

        // Ahmed bought ALL three laptops (Q16)
        AddOrder(ahmed, new DateTime(2025, 1, 15), OrderStatus.Delivered, (proLaptop, 1, 0m), (mouse, 2, 0.10m));
        AddOrder(ahmed, new DateTime(2025, 2, 10), OrderStatus.Delivered, (airLaptop, 1, 0.05m), (sleeve, 1, 0m));
        AddOrder(ahmed, new DateTime(2025, 3, 5), OrderStatus.Delivered, (gamingLaptop, 1, 0m), (hub, 3, 0m));
        AddOrder(ahmed, new DateTime(2026, 2, 14), OrderStatus.Delivered, (hub, 1, 0m));

        AddOrder(sara, new DateTime(2025, 2, 20), OrderStatus.Delivered, (proLaptop, 2, 0.10m), (keyboard, 1, 0m), (airLaptop, 1, 0m));   // Sara: 2 of 3 laptops (Q16 edge case)
        AddOrder(sara, new DateTime(2025, 4, 12), OrderStatus.Delivered, (phoneAlpha, 1, 0m), (hub, 2, 0m));

        AddOrder(omar, new DateTime(2025, 3, 18), OrderStatus.Delivered, (monitor27, 2, 0m), (mouse, 1, 0m), (keyboard, 1, 0m));
        AddOrder(omar, new DateTime(2025, 5, 22), OrderStatus.Shipped, (monitor4k, 1, 0m));

        AddOrder(mona, new DateTime(2025, 4, 2), OrderStatus.Delivered, (phoneAlpha, 1, 0.05m), (phoneBeta, 1, 0m));
        // Pending quantities are bigger than stock for mouse (3) and keyboard (4) -> Q13
        AddOrder(mona, new DateTime(2025, 6, 14), OrderStatus.Pending, (mouse, 5, 0m), (keyboard, 6, 0m));
        // Cancelled 11 months ago: NOT older than one year, so Q25 must keep it
        AddOrder(mona, now.AddMonths(-11), OrderStatus.Cancelled, (airLaptop, 1, 0m));

        // Youssef cancelled 3 orders (Q6), all OLDER than one year (Q25 must delete them).
        // Dates are relative to today so the data stays correct whenever you run it.
        AddOrder(youssef, now.AddMonths(-20), OrderStatus.Cancelled, (monitor27, 1, 0m));
        AddOrder(youssef, now.AddMonths(-16), OrderStatus.Cancelled, (phoneBeta, 1, 0m));
        AddOrder(youssef, now.AddMonths(-13), OrderStatus.Cancelled, (hub, 1, 0m));
        AddOrder(youssef, new DateTime(2025, 9, 9), OrderStatus.Delivered, (proLaptop, 1, 0m), (sleeve, 2, 0m));

        AddOrder(nour, new DateTime(2025, 6, 20), OrderStatus.Delivered, (monitor4k, 2, 0.10m), (phoneBeta, 2, 0m));
        AddOrder(nour, new DateTime(2025, 9, 30), OrderStatus.Delivered, (airLaptop, 1, 0m), (mouse, 1, 0m), (cableAccessory, 3, 0m));

        AddOrder(laila, new DateTime(2025, 8, 15), OrderStatus.Delivered, (phoneAlpha, 2, 0m), (monitor27, 1, 0m), (cablePhone, 2, 0m));
        AddOrder(laila, new DateTime(2025, 11, 11), OrderStatus.Delivered, (proLaptop, 1, 0.05m), (keyboard, 1, 0m));

        // Karim: recent customer with one delivered and one (recent) cancelled order
        AddOrder(karim, now.AddDays(-25), OrderStatus.Delivered, (phoneAlpha, 1, 0m));
        AddOrder(karim, now.AddDays(-20), OrderStatus.Cancelled, (phoneBeta, 1, 0m));

        // Salma: only a cancelled order -> has an order, but no purchases (Q14, Q18, Q21 edge case)
        AddOrder(salma, now.AddDays(-40), OrderStatus.Cancelled, (phoneAlpha, 1, 0m));

        // ---------- Reviews ----------
        var reviews = new List<Review>();

        // Pro Laptop 15: 10 reviews, average 4.8 -> Q11
        int[] proLaptopRatings = { 5, 5, 5, 5, 4, 5, 5, 4, 5, 5 };
        for (int i = 0; i < proLaptopRatings.Length; i++)
        {
            reviews.Add(new Review
            {
                Product = proLaptop,
                Customer = customers[i],
                Rating = proLaptopRatings[i],
                Comment = "Review of Pro Laptop 15 by " + customers[i].FullName,
                CreatedAt = now.AddDays(-30 - i)
            });
        }

        // Karim, Hana and Tarek reviewed Pro Laptop 15 above but never bought it -> Q17

        // Phone Alpha: 10 reviews but average 3.6 -> must NOT match Q11 (average too low)
        int[] phoneAlphaRatings = { 4, 3, 4, 3, 4, 3, 4, 4, 4, 3 };
        for (int i = 0; i < phoneAlphaRatings.Length; i++)
        {
            reviews.Add(new Review
            {
                Product = phoneAlpha,
                Customer = customers[i],
                Rating = phoneAlphaRatings[i],
                Comment = "Review of Phone Alpha by " + customers[i].FullName,
                CreatedAt = now.AddDays(-60 - i)
            });
        }

        // Monitor 27: average 5.0 but only 3 reviews -> must NOT match Q11 (not enough reviews)
        for (int i = 1; i <= 3; i++)
        {
            reviews.Add(new Review
            {
                Product = monitor27,
                Customer = customers[i],
                Rating = 5,
                Comment = "Review of Monitor 27 by " + customers[i].FullName,
                CreatedAt = now.AddDays(-90 - i)
            });
        }

        // ---------- Save ----------
        db.Categories.AddRange(laptops, accessories, phones, monitors, books);
        db.Products.AddRange(products);
        db.Customers.AddRange(customers);
        db.Orders.AddRange(orders);
        db.Reviews.AddRange(reviews);
        await db.SaveChangesAsync();

        Console.WriteLine("Sample data inserted.");
    }
}

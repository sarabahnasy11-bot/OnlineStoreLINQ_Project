using Microsoft.EntityFrameworkCore;
using OnlineStoreLINQ.Data;
using OnlineStoreLINQ.Queries;

try
{
    // 1) Make sure we have a REAL server name (appsettings.json may still contain YOUR_SERVER_NAME)
    var connectionString = ConnectionHelper.Get();
    if (ConnectionHelper.IsPlaceholder(connectionString))
    {
        Console.WriteLine("appsettings.json still contains YOUR_SERVER_NAME (or is empty).");
        Console.WriteLine("Type your SQL Server name (the one you use in SSMS), for example:");
        Console.WriteLine("   (localdb)\\MSSQLLocalDB     or     localhost\\SQLEXPRESS");
        Console.Write("Server name (empty = exit): ");
        var server = (Console.ReadLine() ?? "").Trim();
        if (server.Length == 0) return;

        // Used only for this run. To make it permanent, edit appsettings.json.
        Environment.SetEnvironmentVariable(
            ConnectionHelper.EnvironmentVariableName, ConnectionHelper.Build(server));
    }

    using var db = new OnlineStoreContext();

    // 2) Create/prepare the database and insert sample data
    await InitializeDatabaseAsync(db);

    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("==================== MENU ====================");
        Console.WriteLine(" 1-23 : run that question (read-only)");
        Console.WriteLine(" 20B  : Q20 alternative (filtered Include) - use only if Q20 fails");
        Console.WriteLine(" 24   : Q24 - UPDATE prices (changes data)");
        Console.WriteLine(" 25   : Q25 - DELETE old cancelled orders (changes data)");
        Console.WriteLine(" A    : run questions 1-23 + bonus demos");
        Console.WriteLine(" B    : bonus demos (b) N+1 and (c) Select with index");
        Console.WriteLine(" R    : reset database (delete, recreate, reseed)");
        Console.WriteLine(" 0    : exit");
        Console.Write("Your choice: ");

        var input = (Console.ReadLine() ?? "").Trim().ToUpperInvariant();

        if (input == "0") break;

        if (input == "A")
        {
            for (int n = 1; n <= 23; n++) await RunQuestion(n, db);
            await RunBonus(db);
        }
        else if (input == "B")
        {
            await RunBonus(db);
        }
        else if (input == "20B")
        {
            await Level3Queries.Question20_FilteredIncludeVersion(db);
        }
        else if (input == "R")
        {
            await db.Database.EnsureDeletedAsync();
            await InitializeDatabaseAsync(db);
            Console.WriteLine("Database was reset.");
        }
        else if (int.TryParse(input, out int number) && number >= 1 && number <= 25)
        {
            await RunQuestion(number, db);
        }
        else
        {
            Console.WriteLine("Invalid choice. Try again.");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("ERROR: " + ex.Message);
    if (ex.InnerException != null)
        Console.WriteLine("Details: " + ex.InnerException.Message);
    Console.WriteLine("Check: server name in appsettings.json, and that you ran Add-Migration + Update-Database.");
}

// ----- helper functions -----

// Uses migrations when the project has them. If you have NOT created a migration yet,
// it creates the tables directly from the model (EnsureCreated) so the program still runs.
static async Task InitializeDatabaseAsync(OnlineStoreContext db)
{
    if (db.Database.GetMigrations().Any())
    {
        Console.WriteLine("Applying migrations (creates the database if needed)...");
        await db.Database.MigrateAsync();
    }
    else
    {
        Console.WriteLine("No migrations found: creating the database from the model (EnsureCreated).");
        Console.WriteLine("(To use migrations later: delete the database, then run Add-Migration InitialCreate.)");
        await db.Database.EnsureCreatedAsync();
    }

    await SeedData.SeedAsync(db);          // only inserts when the database is empty
}

static async Task RunQuestion(int number, OnlineStoreContext db)
{
    switch (number)
    {
        case 1: await Level1Queries.Question01(db); break;
        case 2: await Level1Queries.Question02(db); break;
        case 3: await Level1Queries.Question03(db); break;
        case 4: await Level1Queries.Question04(db); break;
        case 5: await Level1Queries.Question05(db); break;
        case 6: await Level1Queries.Question06(db); break;
        case 7: await Level2Queries.Question07(db); break;
        case 8: await Level2Queries.Question08(db); break;     // also prints SQL (Bonus a)
        case 9: await Level2Queries.Question09(db); break;
        case 10: await Level2Queries.Question10(db); break;
        case 11: await Level2Queries.Question11(db); break;
        case 12: await Level2Queries.Question12(db); break;
        case 13: await Level2Queries.Question13(db); break;
        case 14: await Level2Queries.Question14(db); break;
        case 15: await Level3Queries.Question15(db); break;
        case 16: await Level3Queries.Question16(db); break;
        case 17: await Level3Queries.Question17(db); break;
        case 18: await Level3Queries.Question18(db); break;
        case 19: await Level3Queries.Question19(db); break;
        case 20: await Level3Queries.Question20(db); break;
        case 21: await Level3Queries.Question21(db); break;
        case 22: await Level3Queries.Question22(db); break;
        case 23: await Level4Methods.Question23(db); break;
        case 24:
            await Level4Methods.ShowAccessoryPrices(db, "before");   // optional display (separate query)
            await Level4Methods.Question24(db);                      // the UPDATE itself
            await Level4Methods.ShowAccessoryPrices(db, "after");    // optional display (separate query)
            break;
        case 25: await Level4Methods.Question25(db); break;
    }
}

static async Task RunBonus(OnlineStoreContext db)
{
    await Level3Queries.Question20_NPlusOneDemo(db);   // Bonus (b)
    Level3Queries.Question22_WhyIndexSelectFails();    // Bonus (c)
    Console.WriteLine("\n(Bonus (a) = the SQL printed inside Question 8)");
}

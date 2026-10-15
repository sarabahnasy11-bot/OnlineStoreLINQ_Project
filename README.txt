OnlineStoreLINQ - project files (reviewed version)
1) Copy Models, Data, Queries, Filters + Program.cs into your Visual Studio project (replace existing files).
   Names must be: Data, Models, Filters, Queries (not Datas / Model / Filter / Querie).
2) NuGet (all three must be 10.0.x, same version): Microsoft.EntityFrameworkCore.SqlServer,
   Microsoft.EntityFrameworkCore.Tools, Microsoft.Extensions.Configuration.Json.
   OnlineStoreLINQ.csproj is included (net10.0, EF Core 10.0.12). If your csproj differs, compare the two.
3) appsettings.json: Copy to Output Directory = Copy if newer.
   You may leave YOUR_SERVER_NAME: the program asks you for the server name when it starts.
4) Nothing else is required: with no migration the program creates the database itself (EnsureCreated).
   OPTIONAL (migrations): Package Manager Console:  Add-Migration InitialCreate  (do this BEFORE the first run,
   or delete the database first, because EnsureCreated and migrations must not be mixed).
5) Ctrl+F5 and use the menu. Q24 / Q25 change data: press R afterwards to reset.

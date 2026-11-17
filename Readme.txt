B2B AGENT PORTAL (WEB) - Software Engineering Lab Assignment 2
ASP.NET Core Razor Pages (.NET 8), Entity Framework Core 8, SQL Server, Bootstrap 5

1. REQUIREMENTS
- Visual Studio 2022 (17.8 or newer) with the workloads:
  "ASP.NET and web development" and "Data storage and processing" (LocalDB)
- .NET 8 SDK (installed with Visual Studio)

2. DATABASE (once)
- Run Database/01_Schema_Seed.sql in SSMS, or from a terminal:
    sqlcmd -S "(localdb)\MSSQLLocalDB" -i Database\01_Schema_Seed.sql
- It creates the database B2BWholesaleDB with all tables and seed data.
- WARNING: running the script again deletes and recreates the data
  (including orders placed from the portal). Use it for a clean reset.

3. CONNECTION STRING
- File: B2BAgentPortal/appsettings.json, key ConnectionStrings:B2BWholesaleDb
- Default: Server=(localdb)\MSSQLLocalDB;Database=B2BWholesaleDB;Trusted_Connection=True;...
  No change needed if you use LocalDB.
- Another SQL Server: change Server=... in that key, or override it with
  .NET user secrets or the environment variable ConnectionStrings__B2BWholesaleDb.
- No password is stored in the repository.
- Other setting: MaxLoginAttempts (default 5) in appsettings.json.

4. RUN
- Visual Studio: open the solution, set B2BAgentPortal as the startup project, press F5.
- Or terminal, inside the B2BAgentPortal folder: dotnet run
- Addresses (from Properties/launchSettings.json):
    https://localhost:7020   http://localhost:5261

5. DEMO ACCOUNTS (demo passwords only)
- Agent:  agent01 / Agent@123   (any of agent01 ... agent14)
- Locked: agent15 / Agent@123   (rejected: account locked)
- Admin:  admin / Admin@123     (rejected here: the portal is for Agents only;
  Admin users log in to the desktop application)

6. WHAT TO TRY
- Catalog (home page): search items; stock below 20 is shown in yellow.
- New Order: add several lines, place the order; the order, its lines and the
  stock deduction are saved in one transaction.
- Insufficient stock: order 6 units of "Muoi i-ot 500g" (stock 5) -> blocked,
  nothing is saved.
- My Orders: only the logged-in agent's orders; the Invoice button opens a
  printable invoice (Ctrl+P or Print / Save as PDF).

7. PROJECT STRUCTURE
- Pages/      Razor Pages (Login, Index = catalog, NewOrder, Orders, Invoice, Logout)
- Services/   OrderService.cs: order transaction (testable business logic)
- Models/ and Data/   EF Core classes scaffolded from the existing database
- Do NOT run EF Core migrations: the SQL script owns the schema.
  If the schema changes, re-scaffold:
    dotnet tool restore
    dotnet ef dbcontext scaffold "Name=ConnectionStrings:B2BWholesaleDb" Microsoft.EntityFrameworkCore.SqlServer --context B2BContext --context-dir Data --output-dir Models
  (re-add your edits afterwards; scaffolding overwrites the model files)

8. SUBMISSION
- GitHub: https://github.com/Narwhal1412/SE-QT2_2
- Video demo (optional): [link]
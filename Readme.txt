==============================================================================
SE LAB ASSIGNMENT 2 - B2B WHOLESALE MANAGEMENT SYSTEM          Group 69, Sem 1_2627
==============================================================================

1. LINKS
   GitHub repository : https://github.com/Narwhal1412/SE-QT2_2
   Video demo        : https://www.youtube.com/watch?v=cqJObVwS7Jc

2. TEAM AND CONTRIBUTION
   Student                       ID         Work                                          %
   Le Anh Tuan                   524H0074   Database, WinForms back-office app (2, 1.1-1.4)   40%
   Nguyen Gia Huy                524H0158   Agent Portal web app (3.1-3.4)                    30%
   Tran Do Quoc Anh              524H0041   Unit tests (5.1), config, Git, report (6)         30%
   Git branches: anhTuan, feature/web-setup, QuocAnh. All merged through Pull Requests.

3. WHAT IS IN THE PACKAGE
   B2BWholesale/B2BAdmin       WinForms back-office application (.NET 8)
   B2BWholesale/Database       01_Schema_Seed.sql (database + seed data)
   B2BAgentPortal              Agent Portal, ASP.NET Core Razor Pages (.NET 8, EF Core 8)
   B2BAgentPortal.Tests        Unit tests (xUnit)
   Report.docx                 Full report with screenshots, diagram, division of labor

4. HOW TO RUN (Visual Studio 2022, .NET 8 SDK, SQL Server LocalDB or SQL Server)
   Step 1  Create the database: open Database/01_Schema_Seed.sql in SSMS and run it once.
           It creates database B2BWholesaleDB, 5 tables and the seed data.
           (Running it again drops and recreates the tables.)
   Step 2  Connection string (stored in config files, never in the code):
           Desktop : B2BWholesale/B2BAdmin/App.config      -> name "B2BWholesaleDb"
           Web     : B2BAgentPortal/appsettings.json       -> ConnectionStrings:B2BWholesaleDb
           The default uses (localdb)\MSSQLLocalDB with Windows authentication,
           so nothing needs to change on a machine with LocalDB.
           For another SQL Server, change only the Server= part in both files.
   Step 3  Desktop app: open B2BWholesale/B2BWholesale.sln, startup project B2BAdmin, press F5.
   Step 4  Web app: open B2BAgentPortal/B2BAgentPortal.csproj, press F5.
           It runs at https://localhost:7020 (or http://localhost:5261).
   Step 5  Unit tests: Test > Run All Tests, or run "dotnet test B2BAgentPortal.Tests".
           No SQL Server needed (tests use an in-memory SQLite database).

5. DEMO ACCOUNTS (dummy passwords)
   Admin (desktop app) : admin   / Admin@123
   Agent (web portal)  : agent01 / Agent@123
   The desktop app rejects Agent accounts and the web portal rejects Admin accounts.
   agent15 is a locked account (to test the lock rule). An account locks after 5 wrong
   passwords (MaxLoginAttempts in the config files).

6. WHERE TO FIND EACH REQUIREMENT
   Database and seed data (10)   Database/01_Schema_Seed.sql    Report section 3
   WinForms Admin (25)           B2BAdmin: Auth (login), Items/Agents/Orders tabs,
                                 Shipping Manifest (print preview), Dashboard   Report section 4
   Agent Portal (35)             Login, Catalog, New Order, My Orders, Invoice  Report section 5
                                 Order transaction: B2BAgentPortal/Services/OrderService.cs
   Unit tests (15)               B2BAgentPortal.Tests (24 tests)                Report section 6
   Configuration, Git, report    Report sections 7, 8, 9

7. NOTES
   - Passwords are stored as SHA-256 hashes. No real password or secret is in the repository.
   - bin and obj folders were removed from SourceCode.zip.
==============================================================================

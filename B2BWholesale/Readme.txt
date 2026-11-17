B2B Wholesale – Desktop (WinForms) – setup
1. Run Database/01_Schema_Seed.sql in SSMS / Azure Data Studio (creates B2BWholesaleDB + seed data).
2. Edit B2BAdmin/App.config -> connectionStrings -> "B2BWholesaleDb" (Server=...).
3. Open B2BWholesale.sln in Visual Studio 2022 (.NET 8 SDK, "Windows Forms" workload), restore NuGet, F5.
Accounts: admin / Admin@123 (Admin) | agent01 / Agent@123 (Agent -> rejected by desktop app) | agent15 is locked.
Account locks after 5 wrong passwords (Users.Lock = 1; unlock with: UPDATE Users SET [Lock]=0, FailedAttempts=0 WHERE UserName='...').

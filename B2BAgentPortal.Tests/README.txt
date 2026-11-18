==============================================================================
UNIT TESTS - B2BAgentPortal.Tests            (Assignment 2, Exercise 5 / Task 5.1)
==============================================================================
Author / owner of this part : <student name - student ID>
Framework                   : xUnit 2.x on .NET 8
Project under test          : B2BAgentPortal (ASP.NET Core Razor Pages, Agent Portal)
Number of tests             : 24 test methods (25 test cases, one Theory has 2 data rows)

------------------------------------------------------------------------------
1. WHAT IS TESTED
------------------------------------------------------------------------------
The tests cover the core business logic of the Agent Portal, not generated code:

  a) Order transaction  (Services/OrderService.cs)            - OrderServiceTests.cs
  b) Login rules        (Pages/Login.cshtml.cs)               - LoginTests.cs
  c) Invoice logic      (Pages/Invoice.cshtml.cs)             - InvoiceTests.cs

------------------------------------------------------------------------------
2. HOW TO RUN
------------------------------------------------------------------------------
Requirements: Visual Studio 2022 (17.8+) or the .NET 8 SDK. Internet access is
needed once to restore NuGet packages. SQL Server is NOT required.

Visual Studio : open the solution/project -> Test > Run All Tests
                (or Test > Test Explorer, then press "Run All").
Command line  : dotnet test B2BAgentPortal.Tests

Expected result: 25 passed, 0 failed.
(Take the screenshot of Test Explorer for Report.docx after the run.)

Nothing to configure: the tests do not read appsettings.json and never touch the
real database, so running them cannot damage or depend on your SQL Server data.

------------------------------------------------------------------------------
3. HOW THE TESTS ARE BUILT
------------------------------------------------------------------------------
- Each test creates its own private in-memory SQLite database (TestDb.cs) and
  throws it away afterwards, so tests are independent and can run in any order.
- SQLite is a real relational engine, so the real EF Core code, raw SQL
  (UPDATE Item ... WHERE StockQuantity >= qty) and database transactions
  (commit / rollback) in OrderService are executed exactly as in production.
  This is why we did not use EF's "InMemory" provider: it ignores transactions
  and raw SQL, so it could not prove the stock-deduction rollback.
- TestDb.cs recreates the tables of Database/01_Schema_Seed.sql (same table and
  column names). If the schema changes, update the Schema string in TestDb.cs.
- Pattern used in every test: Arrange (seed data) -> Act (call the code) ->
  Assert (check the result AND re-read the database with a second context to
  prove what was really saved).

------------------------------------------------------------------------------
4. TEST LIST
------------------------------------------------------------------------------
OrderServiceTests.cs  - merge logic (no database)
  MergeLines_SameItemTwice_SumsQuantitiesIntoOneLine
  MergeLines_ZeroOrNegativeQuantity_IsDropped            (Theory: 0, -4)
  MergeLines_EmptyInput_ReturnsEmptyList
  MergeLines_DistinctItems_KeepsAllLinesInOrder
  MergeLines_DuplicateWithNonPositiveLine_IgnoresOnlyTheBadLine

OrderServiceTests.cs  - order transaction (stock deduction)
  PlaceOrder_EnoughStock_CreatesOrderDetailsAndDeductsStock
  PlaceOrder_InsufficientStock_IsBlockedAndNothingIsSaved
  PlaceOrder_SecondLineOutOfStock_RollsBackTheFirstLineToo   (atomicity)
  PlaceOrder_ExactlyAllRemainingStock_Succeeds_AndStockBecomesZero
  PlaceOrder_DuplicateLines_AreMergedAndDeductedTogether
  PlaceOrder_DuplicateLinesExceedingStock_AreBlocked
  PlaceOrder_UnitAmountIsCopiedFromCurrentItemPrice
  PlaceOrder_NoLines_ReturnsErrorWithoutTouchingTheDatabase
  PlaceOrder_UnknownItem_IsBlockedAndOrderIsRolledBack

LoginTests.cs         - user lockout validation
  WrongPassword_IncrementsFailedAttempts_ButDoesNotLockYet
  ReachingMaxFailedAttempts_LocksTheAccount
  LockedAccount_IsRejected_EvenWithTheCorrectPassword
  UnknownUser_GetsTheSameGenericMessageAsAWrongPassword
  AdminRole_WithCorrectPassword_IsDeniedAccessToTheAgentPortal
  PasswordHash_MatchesTheValueStoredInTheSeedScript

InvoiceTests.cs       - order totals and data isolation
  Total_IsTheSumOfQuantityTimesUnitAmount
  Total_OfAnOrderWithoutLines_IsZero
  Invoice_OfAnotherAgent_ReturnsNotFound
  Invoice_OfTheLoggedInAgent_IsShownWithCorrectTotal

------------------------------------------------------------------------------
5. SCOPE AND KNOWN LIMITS
------------------------------------------------------------------------------
- The WinForms back-office app (B2BAdmin) is not unit tested: its logic is in
  static classes tied to SQL Server and to UI forms. The assignment asks for at
  least 5 meaningful tests of core business logic, which the web portal covers.
- A successful web login is not tested because it needs the full ASP.NET
  authentication pipeline (SignInAsync). The lockout and role checks, which are
  the business rules, are tested through their failure paths.
- UI pages (Razor views, Bootstrap, JavaScript totals) are not unit tested.

------------------------------------------------------------------------------
6. TROUBLESHOOTING
------------------------------------------------------------------------------
- "Package restore failed"      : check internet / NuGet source, then rebuild.
- Tests do not appear           : build the solution once, then refresh Test Explorer.
- A test fails after a code change in OrderService / LoginModel / InvoiceModel:
  the test is describing the intended business rule - fix the code or, if the
  rule really changed, update the test and mention it in the Pull Request.
- Schema changed in the SQL script: update the Schema string in TestDb.cs.

------------------------------------------------------------------------------
7. GIT
------------------------------------------------------------------------------
Branch : feature/unit-tests  ->  Pull Request into main
Commit the folder B2BAgentPortal.Tests (without bin/ and obj/).
==============================================================================

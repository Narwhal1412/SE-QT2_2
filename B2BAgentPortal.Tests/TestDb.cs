using B2BAgentPortal.Data;
using B2BAgentPortal.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace B2BAgentPortal.Tests;

/// <summary>
/// One private in-memory SQLite database per test. The connection stays open for the
/// lifetime of the object (an in-memory database disappears when its connection closes).
/// The schema mirrors Database/01_Schema_Seed.sql with the same table and column names,
/// so the raw SQL in OrderService runs unchanged.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _conn;

    private const string Schema = @"
        CREATE TABLE Agent(AgentID INTEGER PRIMARY KEY AUTOINCREMENT, AgentName TEXT NOT NULL, Address TEXT, Phone TEXT);
        CREATE TABLE Item(ItemID INTEGER PRIMARY KEY AUTOINCREMENT, ItemName TEXT NOT NULL, Size TEXT,
                          Price TEXT NOT NULL, StockQuantity INTEGER NOT NULL DEFAULT 0 CHECK(StockQuantity >= 0));
        CREATE TABLE ""Order""(OrderID INTEGER PRIMARY KEY AUTOINCREMENT, OrderDate TEXT NOT NULL,
                          AgentID INTEGER NOT NULL, OrderStatus TEXT NOT NULL DEFAULT 'Pending');
        CREATE TABLE OrderDetail(ID INTEGER PRIMARY KEY AUTOINCREMENT, OrderID INTEGER NOT NULL, ItemID INTEGER NOT NULL,
                          Quantity INTEGER NOT NULL, UnitAmount TEXT NOT NULL, UNIQUE(OrderID, ItemID));
        CREATE TABLE Users(UserID INTEGER PRIMARY KEY AUTOINCREMENT, UserName TEXT NOT NULL UNIQUE, Email TEXT NOT NULL UNIQUE,
                          Password TEXT NOT NULL, ""Lock"" INTEGER NOT NULL DEFAULT 0, FailedAttempts INTEGER NOT NULL DEFAULT 0,
                          Role TEXT NOT NULL, AgentID INTEGER);";

    public TestDb()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = Schema;
        cmd.ExecuteNonQuery();
    }

    /// <summary>A fresh context on the same database (use a second one to check what was really saved).</summary>
    public B2BContext NewContext() =>
        new(new DbContextOptionsBuilder<B2BContext>().UseSqlite(_conn).Options);

    // Same format as Users.Password in the real database: SHA-256, uppercase hex
    public static string Hash(string pw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pw)));

    // ---------- seed helpers ----------

    public int AddAgent(string name = "Test Agent")
    {
        using var db = NewContext();
        var a = new Agent { AgentName = name };
        db.Agents.Add(a);
        db.SaveChanges();
        return a.AgentId;
    }

    public int AddItem(string name, decimal price, int stock)
    {
        using var db = NewContext();
        var i = new Item { ItemName = name, Price = price, StockQuantity = stock };
        db.Items.Add(i);
        db.SaveChanges();
        return i.ItemId;
    }

    public int AddUser(string userName, string password, string role,
                       bool locked = false, int failedAttempts = 0, int? agentId = null)
    {
        using var db = NewContext();
        var u = new User
        {
            UserName = userName, Email = userName + "@test.vn", Password = Hash(password),
            Role = role, Lock = locked, FailedAttempts = failedAttempts, AgentId = agentId
        };
        db.Users.Add(u);
        db.SaveChanges();
        return u.UserId;
    }

    public int AddOrder(int agentId, params (int itemId, int qty, decimal unitAmount)[] lines)
    {
        using var db = NewContext();
        var o = new Order { AgentId = agentId, OrderDate = DateTime.Now, OrderStatus = "Pending" };
        db.Orders.Add(o);
        db.SaveChanges();
        foreach (var (itemId, qty, unit) in lines)
            db.OrderDetails.Add(new OrderDetail { OrderId = o.OrderId, ItemId = itemId, Quantity = qty, UnitAmount = unit });
        db.SaveChanges();
        return o.OrderId;
    }

    public int StockOf(int itemId)
    {
        using var db = NewContext();
        return db.Items.AsNoTracking().Single(i => i.ItemId == itemId).StockQuantity;
    }

    public void Dispose() => _conn.Dispose();
}

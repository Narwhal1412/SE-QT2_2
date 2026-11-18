using B2BAgentPortal.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace B2BAgentPortal.Tests;

/// <summary>
/// Login rules of the web portal: wrong-password counting, account lockout, locked accounts,
/// and "only the Agent role may enter". Only failure paths are exercised here, because a
/// successful login needs the full ASP.NET authentication pipeline (SignInAsync).
/// </summary>
public class LoginTests
{
    private const int MaxAttempts = 3;

    private static LoginModel NewLoginModel(TestDb test, string username, string password)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MaxLoginAttempts"] = MaxAttempts.ToString() })
            .Build();
        return new LoginModel(test.NewContext(), config) { Username = username, Password = password };
    }

    private static async Task<(int failed, bool locked)> StateOf(TestDb test, string username)
    {
        using var db = test.NewContext();
        var u = await db.Users.AsNoTracking().SingleAsync(x => x.UserName == username);
        return (u.FailedAttempts, u.Lock);
    }

    [Fact]
    public async Task WrongPassword_IncrementsFailedAttempts_ButDoesNotLockYet()
    {
        using var test = new TestDb();
        test.AddUser("agent01", "Agent@123", "Agent");

        var page = NewLoginModel(test, "agent01", "wrong");
        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("Invalid username or password.", page.Error);
        Assert.Equal((1, false), await StateOf(test, "agent01"));
    }

    [Fact]
    public async Task ReachingMaxFailedAttempts_LocksTheAccount()
    {
        using var test = new TestDb();
        test.AddUser("agent01", "Agent@123", "Agent");

        for (int i = 0; i < MaxAttempts; i++)
            await NewLoginModel(test, "agent01", "wrong").OnPostAsync();

        Assert.Equal((MaxAttempts, true), await StateOf(test, "agent01"));
    }

    [Fact]
    public async Task LockedAccount_IsRejected_EvenWithTheCorrectPassword()
    {
        using var test = new TestDb();
        test.AddUser("agent15", "Agent@123", "Agent", locked: true);

        var page = NewLoginModel(test, "agent15", "Agent@123");
        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("This account is locked.", page.Error);
    }

    [Fact]
    public async Task UnknownUser_GetsTheSameGenericMessageAsAWrongPassword()
    {
        using var test = new TestDb();
        test.AddUser("agent01", "Agent@123", "Agent");

        var unknown = NewLoginModel(test, "nobody", "whatever");
        await unknown.OnPostAsync();
        var wrongPw = NewLoginModel(test, "agent01", "wrong");
        await wrongPw.OnPostAsync();

        Assert.Equal(wrongPw.Error, unknown.Error);   // does not reveal which usernames exist
    }

    [Fact]
    public async Task AdminRole_WithCorrectPassword_IsDeniedAccessToTheAgentPortal()
    {
        using var test = new TestDb();
        test.AddUser("admin", "Admin@123", "Admin");

        var page = NewLoginModel(test, "admin", "Admin@123");
        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);                       // stays on the login page
        Assert.Contains("Access denied", page.Error);
        Assert.False((await StateOf(test, "admin")).locked);     // a role error is not a lockout
    }

    [Fact]
    public void PasswordHash_MatchesTheValueStoredInTheSeedScript()
    {
        // 'Admin@123' hash as written in Database/01_Schema_Seed.sql.
        // LoginModel.Hash is private, so it is called through reflection.
        var hash = typeof(LoginModel).GetMethod("Hash", BindingFlags.NonPublic | BindingFlags.Static)!;
        var actual = (string)hash.Invoke(null, new object[] { "Admin@123" })!;

        Assert.Equal("E86F78A8A3CAF0B60D8E74E5942AA6D86DC150CD3C03338AEF25B7D2D7E3ACC7", actual);
    }
}

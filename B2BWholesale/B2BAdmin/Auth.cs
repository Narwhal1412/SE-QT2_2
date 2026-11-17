using System.Configuration;
using System.Security.Cryptography;
using System.Text;
namespace B2BAdmin;

public static class Auth
{
    public static string CurrentUser { get; private set; }

    public static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    static int MaxAttempts => int.TryParse(ConfigurationManager.AppSettings["MaxLoginAttempts"], out var n) ? n : 5;

    public static (bool ok, string msg) Login(string user, string pass)
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrEmpty(pass)) return (false, "Enter username and password.");
        var dt = Db.Query("SELECT UserID,Password,[Lock],Role FROM Users WHERE UserName=@u", ("@u", user.Trim()));
        if (dt.Rows.Count == 0) return (false, "Invalid username or password.");
        var r = dt.Rows[0]; int id = (int)r["UserID"];
        if ((bool)r["Lock"]) return (false, "This account is locked. Contact a system administrator.");
        if (!string.Equals((string)r["Password"], Hash(pass), StringComparison.OrdinalIgnoreCase))
        {
            Db.Exec("UPDATE Users SET FailedAttempts=FailedAttempts+1, [Lock]=CASE WHEN FailedAttempts+1>=@m THEN 1 ELSE [Lock] END WHERE UserID=@id",
                    ("@m", MaxAttempts), ("@id", id));
            return (false, "Invalid username or password.");
        }
        if ((string)r["Role"] != "Admin") return (false, "Access denied: only Admin users may use this application.");
        Db.Exec("UPDATE Users SET FailedAttempts=0 WHERE UserID=@id", ("@id", id));
        CurrentUser = user.Trim();
        return (true, null);
    }
}

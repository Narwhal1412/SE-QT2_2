using System.Configuration;
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
namespace B2BAdmin;

public static class Db
{
    public static readonly string[] Statuses = { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

    public static string ConnectionString =>
        ConfigurationManager.ConnectionStrings["B2BWholesaleDb"]?.ConnectionString
        ?? throw new InvalidOperationException("Missing connection string 'B2BWholesaleDb' in App.config.");

    public static SqlConnection Open() { var c = new SqlConnection(ConnectionString); c.Open(); return c; }

    public static SqlCommand Cmd(SqlConnection c, SqlTransaction tx, string sql, params (string n, object v)[] ps)
    {
        var cmd = new SqlCommand(sql, c, tx);
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
        return cmd;
    }

    public static DataTable Query(string sql, params (string n, object v)[] ps)
    {
        using var c = Open(); using var cmd = Cmd(c, null, sql, ps);
        var dt = new DataTable(); new SqlDataAdapter(cmd).Fill(dt); return dt;
    }

    public static int Exec(string sql, params (string n, object v)[] ps)
    {
        using var c = Open(); using var cmd = Cmd(c, null, sql, ps); return cmd.ExecuteNonQuery();
    }

    public static object Scalar(string sql, params (string n, object v)[] ps)
    {
        using var c = Open(); using var cmd = Cmd(c, null, sql, ps); return cmd.ExecuteScalar();
    }

    public static string Money(decimal v) => v.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " ₫";

    /// <summary>Change order status. Cancelling returns reserved stock to inventory (single transaction).</summary>
    public static void UpdateOrderStatus(int orderId, string newStatus)
    {
        using var c = Open(); using var tx = c.BeginTransaction();
        var old = (string)Cmd(c, tx, "SELECT OrderStatus FROM [Order] WHERE OrderID=@id", ("@id", orderId)).ExecuteScalar();
        if (old == newStatus) return;
        if (old == "Cancelled") throw new InvalidOperationException("A cancelled order cannot be reopened.");
        if (newStatus == "Cancelled")
            Cmd(c, tx, @"UPDATE i SET i.StockQuantity = i.StockQuantity + d.Quantity
                         FROM Item i JOIN OrderDetail d ON d.ItemID = i.ItemID WHERE d.OrderID=@id", ("@id", orderId)).ExecuteNonQuery();
        Cmd(c, tx, "UPDATE [Order] SET OrderStatus=@s WHERE OrderID=@id", ("@s", newStatus), ("@id", orderId)).ExecuteNonQuery();
        tx.Commit();
    }
}

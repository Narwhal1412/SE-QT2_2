using System.Data;
using System.Configuration;
namespace B2BAdmin;

public class DashboardPanel : UserControl, IReloadable
{
    readonly DataGridView gBest = Ui.Grid(), gLow = Ui.Grid(), gRev = Ui.Grid();
    readonly NumericUpDown numTop = new() { Width = 60, Minimum = 1, Maximum = 100, Value = 10 };
    readonly NumericUpDown numLow = new() { Width = 70, Maximum = 100000 };
    readonly ComboBox cmbAgent = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly DateTimePicker dtFrom = new() { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-6) };
    readonly DateTimePicker dtTo = new() { Width = 110, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
    readonly Label lblTotal = new() { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(12, 8, 0, 0) };

    public DashboardPanel()
    {
        numLow.Value = int.TryParse(ConfigurationManager.AppSettings["LowStockThreshold"], out var t) ? t : 20;
        Controls.Add(Ui.Layout(
            (Group("Best-selling items (excludes cancelled orders)", Ui.Row(Ui.L("Top"), numTop, Ui.B("Apply", (s, e) => LoadBest())), gBest), true),
            (Group("Low-stock warning", Ui.Row(Ui.L("Stock below"), numLow, Ui.B("Apply", (s, e) => LoadLow())), gLow), true),
            (Group("Revenue by agent (excludes cancelled orders)", Ui.Row(Ui.L("Agent"), cmbAgent, Ui.L("From"), dtFrom, Ui.L("To"), dtTo, Ui.B("Apply", (s, e) => LoadRev()), lblTotal), gRev), true)));
        Reload();
    }

    static GroupBox Group(string title, Control filter, Control grid)
    {
        var g = new GroupBox { Text = title };
        g.Controls.Add(Ui.Layout((filter, false), (grid, true))); return g;
    }

    public void Reload()
    {
        try
        {
            var ag = Db.Query("SELECT AgentID,AgentName FROM Agent ORDER BY AgentName");
            var row = ag.NewRow(); row["AgentID"] = 0; row["AgentName"] = "-- All agents --"; ag.Rows.InsertAt(row, 0);
            int keep = cmbAgent.SelectedValue is int v ? v : 0;
            cmbAgent.DataSource = ag; cmbAgent.DisplayMember = "AgentName"; cmbAgent.ValueMember = "AgentID"; cmbAgent.SelectedValue = keep;
            LoadBest(); LoadLow(); LoadRev();
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void LoadBest()
    {
        try
        {
            Ui.Bind(gBest, Db.Query(@"SELECT TOP (@n) i.ItemID,i.ItemName,i.Size,SUM(d.Quantity) AS UnitsSold,SUM(d.Quantity*d.UnitAmount) AS Revenue
                FROM OrderDetail d JOIN [Order] o ON o.OrderID=d.OrderID JOIN Item i ON i.ItemID=d.ItemID
                WHERE o.OrderStatus<>'Cancelled' GROUP BY i.ItemID,i.ItemName,i.Size ORDER BY UnitsSold DESC", ("@n", (int)numTop.Value)), "UnitsSold", "Revenue");
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void LoadLow()
    {
        try
        {
            Ui.Bind(gLow, Db.Query("SELECT ItemID,ItemName,Size,StockQuantity FROM Item WHERE StockQuantity<@t ORDER BY StockQuantity", ("@t", (int)numLow.Value)), "StockQuantity");
            foreach (DataGridViewRow r in gLow.Rows) { r.DefaultCellStyle.BackColor = Color.MistyRose; r.DefaultCellStyle.ForeColor = Color.DarkRed; }
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void LoadRev()
    {
        try
        {
            var dt = Db.Query(@"SELECT a.AgentID,a.AgentName,COUNT(DISTINCT o.OrderID) AS Orders,SUM(d.Quantity*d.UnitAmount) AS Revenue
                FROM Agent a JOIN [Order] o ON o.AgentID=a.AgentID JOIN OrderDetail d ON d.OrderID=o.OrderID
                WHERE o.OrderStatus<>'Cancelled' AND o.OrderDate>=@f AND o.OrderDate<@t AND (@a=0 OR a.AgentID=@a)
                GROUP BY a.AgentID,a.AgentName ORDER BY Revenue DESC",
                ("@f", dtFrom.Value.Date), ("@t", dtTo.Value.Date.AddDays(1)), ("@a", cmbAgent.SelectedValue is int v ? v : 0));
            Ui.Bind(gRev, dt, "Orders", "Revenue");
            lblTotal.Text = "Total revenue: " + Db.Money(dt.AsEnumerable().Sum(r => Convert.ToDecimal(r["Revenue"])));
        }
        catch (Exception ex) { Ui.Err(ex); }
    }
}

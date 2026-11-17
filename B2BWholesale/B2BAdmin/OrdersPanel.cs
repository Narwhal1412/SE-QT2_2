namespace B2BAdmin;

public class OrdersPanel : UserControl, IReloadable
{
    readonly DataGridView gridOrders = Ui.Grid(), gridDetail = Ui.Grid();
    readonly ComboBox cmbFilter = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox cmbStatus = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };

    public OrdersPanel()
    {
        cmbFilter.Items.Add("All"); cmbFilter.Items.AddRange(Db.Statuses); cmbFilter.SelectedIndex = 0;
        cmbStatus.Items.AddRange(Db.Statuses); cmbStatus.SelectedIndex = 0;
        var top = Ui.Row(Ui.L("Status filter:"), cmbFilter, Ui.B("Refresh", (s, e) => Reload()));
        var bottom = Ui.Row(Ui.L("Set status to:"), cmbStatus, Ui.B("Update Status", UpdateStatus), Ui.B("Print Shipping Manifest", Print));
        Controls.Add(Ui.Layout((top, false), (gridOrders, true), (gridDetail, true), (bottom, false)));
        cmbFilter.SelectedIndexChanged += (s, e) => Reload();
        gridOrders.SelectionChanged += (s, e) => LoadDetail();
        Reload();
    }

    int? SelectedId => gridOrders.CurrentRow == null ? null : Convert.ToInt32(gridOrders.CurrentRow.Cells["OrderID"].Value);

    public void Reload()
    {
        try
        {
            int? keep = SelectedId;
            Ui.Bind(gridOrders, Db.Query(@"SELECT o.OrderID,o.OrderDate,a.AgentName,o.OrderStatus,ISNULL(SUM(d.Quantity*d.UnitAmount),0) AS Total
                FROM [Order] o JOIN Agent a ON a.AgentID=o.AgentID LEFT JOIN OrderDetail d ON d.OrderID=o.OrderID
                WHERE (@s='All' OR o.OrderStatus=@s)
                GROUP BY o.OrderID,o.OrderDate,a.AgentName,o.OrderStatus ORDER BY o.OrderDate DESC", ("@s", cmbFilter.SelectedItem.ToString())), "Total");
            if (keep != null)
                foreach (DataGridViewRow r in gridOrders.Rows)
                    if (Convert.ToInt32(r.Cells["OrderID"].Value) == keep) { gridOrders.CurrentCell = r.Cells[0]; break; }
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void LoadDetail()
    {
        if (SelectedId == null) { gridDetail.DataSource = null; return; }
        try
        {
            Ui.Bind(gridDetail, Db.Query(@"SELECT i.ItemName,i.Size,d.Quantity,d.UnitAmount,d.Quantity*d.UnitAmount AS LineTotal
                FROM OrderDetail d JOIN Item i ON i.ItemID=d.ItemID WHERE d.OrderID=@id ORDER BY d.ID", ("@id", SelectedId)), "Quantity", "UnitAmount", "LineTotal");
            cmbStatus.SelectedItem = gridOrders.CurrentRow.Cells["OrderStatus"].Value?.ToString();
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void UpdateStatus(object s, EventArgs e)
    {
        if (SelectedId == null) { Ui.Warn("Select an order first."); return; }
        if (cmbStatus.SelectedItem.ToString() == "Cancelled" && !Ui.Confirm("Cancel this order? Stock will be returned to inventory.")) return;
        try { Db.UpdateOrderStatus(SelectedId.Value, cmbStatus.SelectedItem.ToString()); Reload(); }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void Print(object s, EventArgs e)
    {
        if (SelectedId == null) { Ui.Warn("Select an order first."); return; }
        try { ManifestPrinter.Preview(SelectedId.Value); } catch (Exception ex) { Ui.Err(ex); }
    }
}

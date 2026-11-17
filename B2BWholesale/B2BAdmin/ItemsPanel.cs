using Microsoft.Data.SqlClient;
namespace B2BAdmin;

public class ItemsPanel : UserControl, IReloadable
{
    readonly DataGridView grid = Ui.Grid();
    readonly TextBox txtSearch = new() { Width = 200 }, txtName = new() { Width = 200 }, txtSize = new() { Width = 110 };
    readonly NumericUpDown numPrice = new() { Width = 110, DecimalPlaces = 2, Maximum = 1_000_000_000, ThousandsSeparator = true };
    readonly NumericUpDown numStock = new() { Width = 80, Maximum = 10_000_000 };

    public ItemsPanel()
    {
        var top = Ui.Row(Ui.L("Search:"), txtSearch, Ui.B("Search", (s, e) => Reload()), Ui.B("Clear", (s, e) => { txtSearch.Clear(); Reload(); }));
        var bottom = Ui.Row(Ui.L("Name"), txtName, Ui.L("Size"), txtSize, Ui.L("Price"), numPrice, Ui.L("Stock"), numStock,
            Ui.B("Add", Add), Ui.B("Update", Update), Ui.B("Delete", Delete), Ui.B("New", (s, e) => ClearForm()));
        Controls.Add(Ui.Layout((top, false), (grid, true), (bottom, false)));
        grid.SelectionChanged += (s, e) => FillForm();
        Reload();
    }

    int? SelectedId => grid.CurrentRow == null ? null : Convert.ToInt32(grid.CurrentRow.Cells["ItemID"].Value);

    public void Reload()
    {
        try
        {
            Ui.Bind(grid, Db.Query("SELECT ItemID,ItemName,Size,Price,StockQuantity FROM Item WHERE ItemName LIKE @s OR Size LIKE @s ORDER BY ItemID",
                ("@s", "%" + txtSearch.Text.Trim() + "%")), "Price", "StockQuantity");
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void FillForm()
    {
        var r = grid.CurrentRow; if (r == null) return;
        txtName.Text = r.Cells["ItemName"].Value?.ToString(); txtSize.Text = r.Cells["Size"].Value?.ToString();
        numPrice.Value = Convert.ToDecimal(r.Cells["Price"].Value); numStock.Value = Convert.ToDecimal(r.Cells["StockQuantity"].Value);
    }

    void ClearForm() { grid.ClearSelection(); txtName.Clear(); txtSize.Clear(); numPrice.Value = 0; numStock.Value = 0; txtName.Focus(); }

    bool Valid() { if (txtName.Text.Trim() == "") { Ui.Warn("Item name is required."); return false; } return true; }

    void Add(object s, EventArgs e)
    {
        if (!Valid()) return;
        try
        {
            Db.Exec("INSERT INTO Item(ItemName,Size,Price,StockQuantity) VALUES(@n,@s,@p,@q)",
                ("@n", txtName.Text.Trim()), ("@s", txtSize.Text.Trim()), ("@p", numPrice.Value), ("@q", (int)numStock.Value));
            Reload(); ClearForm();
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void Update(object s, EventArgs e)
    {
        if (SelectedId == null) { Ui.Warn("Select an item first."); return; }
        if (!Valid()) return;
        try
        {
            Db.Exec("UPDATE Item SET ItemName=@n,Size=@s,Price=@p,StockQuantity=@q WHERE ItemID=@id",
                ("@n", txtName.Text.Trim()), ("@s", txtSize.Text.Trim()), ("@p", numPrice.Value), ("@q", (int)numStock.Value), ("@id", SelectedId));
            int id = SelectedId.Value; Reload();
            foreach (DataGridViewRow r in grid.Rows) if (Convert.ToInt32(r.Cells["ItemID"].Value) == id) { grid.CurrentCell = r.Cells[1]; break; }
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void Delete(object s, EventArgs e)
    {
        if (SelectedId == null) { Ui.Warn("Select an item first."); return; }
        if (!Ui.Confirm("Delete this item?")) return;
        try { Db.Exec("DELETE FROM Item WHERE ItemID=@id", ("@id", SelectedId)); Reload(); ClearForm(); }
        catch (SqlException ex) when (ex.Number == 547) { Ui.Warn("This item appears in existing orders and cannot be deleted. Set its stock to 0 instead."); }
        catch (Exception ex) { Ui.Err(ex); }
    }
}

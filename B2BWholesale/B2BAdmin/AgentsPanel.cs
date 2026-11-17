using Microsoft.Data.SqlClient;
namespace B2BAdmin;

public class AgentsPanel : UserControl, IReloadable
{
    readonly DataGridView grid = Ui.Grid();
    readonly TextBox txtSearch = new() { Width = 200 }, txtName = new() { Width = 180 }, txtAddr = new() { Width = 260 }, txtPhone = new() { Width = 110 };
    readonly TextBox txtUser = new() { Width = 110 }, txtMail = new() { Width = 160 }, txtPass = new() { Width = 110, UseSystemPasswordChar = true };

    public AgentsPanel()
    {
        var top = Ui.Row(Ui.L("Search:"), txtSearch, Ui.B("Search", (s, e) => Reload()), Ui.B("Clear", (s, e) => { txtSearch.Clear(); Reload(); }));
        var r1 = Ui.Row(Ui.L("Name"), txtName, Ui.L("Address"), txtAddr, Ui.L("Phone"), txtPhone);
        var r2 = Ui.Row(Ui.L("Portal login (optional, new agent only) – Username"), txtUser, Ui.L("Email"), txtMail, Ui.L("Password"), txtPass);
        var r3 = Ui.Row(Ui.B("Register Agent", Add), Ui.B("Update", Update), Ui.B("Delete", Delete), Ui.B("New", (s, e) => ClearForm()));
        var bottom = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
        bottom.Controls.AddRange(new Control[] { r1, r2, r3 });
        Controls.Add(Ui.Layout((top, false), (grid, true), (bottom, false)));
        grid.SelectionChanged += (s, e) => FillForm();
        Reload();
    }

    int? SelectedId => grid.CurrentRow == null ? null : Convert.ToInt32(grid.CurrentRow.Cells["AgentID"].Value);

    public void Reload()
    {
        try
        {
            Ui.Bind(grid, Db.Query(@"SELECT a.AgentID,a.AgentName,a.Address,a.Phone,u.UserName AS PortalLogin
                FROM Agent a LEFT JOIN Users u ON u.AgentID=a.AgentID
                WHERE a.AgentName LIKE @s OR a.Address LIKE @s OR a.Phone LIKE @s ORDER BY a.AgentID", ("@s", "%" + txtSearch.Text.Trim() + "%")));
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void FillForm()
    {
        var r = grid.CurrentRow; if (r == null) return;
        txtName.Text = r.Cells["AgentName"].Value?.ToString(); txtAddr.Text = r.Cells["Address"].Value?.ToString(); txtPhone.Text = r.Cells["Phone"].Value?.ToString();
    }

    void ClearForm() { grid.ClearSelection(); foreach (var t in new[] { txtName, txtAddr, txtPhone, txtUser, txtMail, txtPass }) t.Clear(); txtName.Focus(); }

    void Add(object s, EventArgs e)
    {
        if (txtName.Text.Trim() == "") { Ui.Warn("Agent name is required."); return; }
        bool login = txtUser.Text.Trim() != "";
        if (login && (txtMail.Text.Trim() == "" || txtPass.Text.Length < 6)) { Ui.Warn("Portal login needs an email and a password of at least 6 characters."); return; }
        try
        {
            using var c = Db.Open(); using var tx = c.BeginTransaction();
            int id = (int)Db.Cmd(c, tx, "INSERT INTO Agent(AgentName,Address,Phone) OUTPUT INSERTED.AgentID VALUES(@n,@a,@p)",
                ("@n", txtName.Text.Trim()), ("@a", txtAddr.Text.Trim()), ("@p", txtPhone.Text.Trim())).ExecuteScalar();
            if (login)
                Db.Cmd(c, tx, "INSERT INTO Users(UserName,Email,Password,Role,AgentID) VALUES(@u,@m,@pw,'Agent',@id)",
                    ("@u", txtUser.Text.Trim()), ("@m", txtMail.Text.Trim()), ("@pw", Auth.Hash(txtPass.Text)), ("@id", id)).ExecuteNonQuery();
            tx.Commit();
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601) { Ui.Warn("Username or email already exists."); return; }
        catch (Exception ex) { Ui.Err(ex); return; }
        Reload(); ClearForm();
    }

    void Update(object s, EventArgs e)
    {
        if (SelectedId == null) { Ui.Warn("Select an agent first."); return; }
        if (txtName.Text.Trim() == "") { Ui.Warn("Agent name is required."); return; }
        try
        {
            Db.Exec("UPDATE Agent SET AgentName=@n,Address=@a,Phone=@p WHERE AgentID=@id",
                ("@n", txtName.Text.Trim()), ("@a", txtAddr.Text.Trim()), ("@p", txtPhone.Text.Trim()), ("@id", SelectedId));
            Reload();
        }
        catch (Exception ex) { Ui.Err(ex); }
    }

    void Delete(object s, EventArgs e)
    {
        if (SelectedId == null) { Ui.Warn("Select an agent first."); return; }
        if (!Ui.Confirm("Delete this agent and their portal login?")) return;
        try { Db.Exec("DELETE FROM Agent WHERE AgentID=@id", ("@id", SelectedId)); Reload(); ClearForm(); }
        catch (SqlException ex) when (ex.Number == 547) { Ui.Warn("This agent has order history and cannot be deleted."); }
        catch (Exception ex) { Ui.Err(ex); }
    }
}

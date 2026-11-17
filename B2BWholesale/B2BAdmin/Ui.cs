using System.Data;
namespace B2BAdmin;

public interface IReloadable { void Reload(); }

public static class Ui
{
    public static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
        BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle
    };

    public static void Bind(DataGridView g, DataTable dt, params string[] numCols)
    {
        g.DataSource = dt;
        foreach (var n in numCols)
            if (g.Columns.Contains(n)) { g.Columns[n].DefaultCellStyle.Format = "N0"; g.Columns[n].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight; }
    }

    public static FlowLayoutPanel Row(params Control[] c)
    {
        var f = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(6), WrapContents = true };
        f.Controls.AddRange(c); return f;
    }

    public static Label L(string t) => new() { Text = t, AutoSize = true, Margin = new Padding(8, 8, 2, 0) };

    public static Button B(string t, EventHandler h) { var b = new Button { Text = t, AutoSize = true, Padding = new Padding(8, 2, 8, 2) }; b.Click += h; return b; }

    public static TableLayoutPanel Layout(params (Control c, bool fill)[] rows)
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows.Length };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int fills = rows.Count(r => r.fill);
        foreach (var (c, f) in rows)
        {
            t.RowStyles.Add(f ? new RowStyle(SizeType.Percent, 100f / fills) : new RowStyle(SizeType.AutoSize));
            c.Dock = DockStyle.Fill; t.Controls.Add(c);
        }
        return t;
    }

    public static void Err(Exception ex) => MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    public static void Warn(string m) => MessageBox.Show(m, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public static bool Confirm(string m) => MessageBox.Show(m, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}

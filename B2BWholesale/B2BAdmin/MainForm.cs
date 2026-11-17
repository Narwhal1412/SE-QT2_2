namespace B2BAdmin;

public class MainForm : Form
{
    public MainForm()
    {
        Text = $"B2B Wholesale – Back Office  |  Logged in as: {Auth.CurrentUser}";
        WindowState = FormWindowState.Maximized; MinimumSize = new Size(1000, 600);
        var tabs = new TabControl { Dock = DockStyle.Fill };
        void Add(string title, UserControl p) { var t = new TabPage(title); p.Dock = DockStyle.Fill; t.Controls.Add(p); tabs.TabPages.Add(t); }
        Add("Dashboard", new DashboardPanel());
        Add("Items", new ItemsPanel());
        Add("Agents", new AgentsPanel());
        Add("Orders", new OrdersPanel());
        tabs.SelectedIndexChanged += (s, e) => (tabs.SelectedTab.Controls[0] as IReloadable)?.Reload();
        Controls.Add(tabs);
    }
}

namespace B2BAdmin;

public class LoginForm : Form
{
    readonly TextBox txtUser = new() { Width = 220 };
    readonly TextBox txtPass = new() { Width = 220, UseSystemPasswordChar = true };

    public LoginForm()
    {
        Text = "B2B Wholesale – Admin Login"; StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(340, 230);
        var title = new Label { Text = "WAREHOUSE ADMIN", Font = new Font(Font.FontFamily, 14, FontStyle.Bold), AutoSize = true, Location = new Point(70, 15) };
        var btn = new Button { Text = "Login", Width = 220, Height = 32, Location = new Point(100, 160) };
        var show = new CheckBox { Text = "Show password", AutoSize = true, Location = new Point(100, 125) };
        show.CheckedChanged += (s, e) => txtPass.UseSystemPasswordChar = !show.Checked;
        Controls.AddRange(new Control[] {
            title,
            new Label { Text = "Username", AutoSize = true, Location = new Point(20, 62) }, Pos(txtUser, 100, 58),
            new Label { Text = "Password", AutoSize = true, Location = new Point(20, 97) }, Pos(txtPass, 100, 93),
            show, btn });
        AcceptButton = btn;
        btn.Click += (s, e) => DoLogin();
    }

    static Control Pos(Control c, int x, int y) { c.Location = new Point(x, y); return c; }

    void DoLogin()
    {
        try
        {
            var (ok, msg) = Auth.Login(txtUser.Text, txtPass.Text);
            if (ok) { DialogResult = DialogResult.OK; Close(); }
            else { Ui.Warn(msg); txtPass.Clear(); txtPass.Focus(); }
        }
        catch (Exception ex) { Ui.Err(ex); }
    }
}

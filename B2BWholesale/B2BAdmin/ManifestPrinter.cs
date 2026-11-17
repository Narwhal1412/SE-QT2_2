using System.Data;
using System.Drawing.Printing;
namespace B2BAdmin;

/// <summary>Shipping Manifest report: print preview dialog (user can print or choose "Microsoft Print to PDF").</summary>
public static class ManifestPrinter
{
    public static void Preview(int orderId)
    {
        var h = Db.Query(@"SELECT o.OrderID,o.OrderDate,o.OrderStatus,a.AgentName,a.Address,a.Phone
            FROM [Order] o JOIN Agent a ON a.AgentID=o.AgentID WHERE o.OrderID=@id", ("@id", orderId));
        if (h.Rows.Count == 0) return;
        var lines = Db.Query(@"SELECT i.ItemName,i.Size,d.Quantity,d.UnitAmount FROM OrderDetail d
            JOIN Item i ON i.ItemID=d.ItemID WHERE d.OrderID=@id ORDER BY d.ID", ("@id", orderId));
        var hr = h.Rows[0]; int idx = 0, page = 0;
        decimal total = lines.AsEnumerable().Sum(r => Convert.ToInt32(r["Quantity"]) * Convert.ToDecimal(r["UnitAmount"]));
        int units = lines.AsEnumerable().Sum(r => Convert.ToInt32(r["Quantity"]));

        var doc = new PrintDocument();
        doc.BeginPrint += (s, e) => { idx = 0; page = 0; };
        doc.PrintPage += (s, e) =>
        {
            var g = e.Graphics; float x = e.MarginBounds.Left, y = e.MarginBounds.Top, w = e.MarginBounds.Width;
            using var big = new Font("Arial", 18, FontStyle.Bold); using var bold = new Font("Arial", 10, FontStyle.Bold); using var f = new Font("Arial", 10);
            page++;
            if (page == 1)
            {
                g.DrawString("SHIPPING MANIFEST", big, Brushes.Black, x, y); y += 40;
                g.DrawString($"Order #: {hr["OrderID"]}      Date: {((DateTime)hr["OrderDate"]):dd/MM/yyyy HH:mm}      Status: {hr["OrderStatus"]}", f, Brushes.Black, x, y); y += 22;
                g.DrawString($"Ship to: {hr["AgentName"]}", bold, Brushes.Black, x, y); y += 20;
                g.DrawString($"Address: {hr["Address"]}", f, Brushes.Black, x, y); y += 20;
                g.DrawString($"Phone: {hr["Phone"]}", f, Brushes.Black, x, y); y += 30;
            }
            float cNo = x, cItem = x + 30, cSize = x + w * 0.45f, cQty = x + w * 0.62f, cPrice = x + w * 0.74f, cAmt = x + w * 0.88f;
            g.DrawString("#", bold, Brushes.Black, cNo, y); g.DrawString("Item", bold, Brushes.Black, cItem, y); g.DrawString("Size", bold, Brushes.Black, cSize, y);
            g.DrawString("Qty", bold, Brushes.Black, cQty, y); g.DrawString("Unit price", bold, Brushes.Black, cPrice, y); g.DrawString("Amount", bold, Brushes.Black, cAmt, y);
            y += 20; g.DrawLine(Pens.Black, x, y, x + w, y); y += 4;
            while (idx < lines.Rows.Count)
            {
                if (y > e.MarginBounds.Bottom - 120) { e.HasMorePages = true; return; }
                var r = lines.Rows[idx]; int q = Convert.ToInt32(r["Quantity"]); decimal p = Convert.ToDecimal(r["UnitAmount"]);
                g.DrawString((idx + 1).ToString(), f, Brushes.Black, cNo, y);
                g.DrawString(r["ItemName"].ToString(), f, Brushes.Black, new RectangleF(cItem, y, cSize - cItem - 4, 18));
                g.DrawString(r["Size"].ToString(), f, Brushes.Black, new RectangleF(cSize, y, cQty - cSize - 4, 18));
                g.DrawString(q.ToString("N0"), f, Brushes.Black, cQty, y);
                g.DrawString(Db.Money(p), f, Brushes.Black, cPrice, y); g.DrawString(Db.Money(q * p), f, Brushes.Black, cAmt, y);
                y += 22; idx++;
            }
            g.DrawLine(Pens.Black, x, y, x + w, y); y += 8;
            g.DrawString($"Total units: {units:N0}", bold, Brushes.Black, x, y);
            g.DrawString($"TOTAL: {Db.Money(total)}", bold, Brushes.Black, cPrice, y); y += 60;
            g.DrawString("Warehouse staff signature", f, Brushes.Black, x, y); g.DrawString("Receiver signature", f, Brushes.Black, x + w * 0.6f, y);
            g.DrawLine(Pens.Black, x, y - 5, x + 180, y - 5); g.DrawLine(Pens.Black, x + w * 0.6f, y - 5, x + w * 0.6f + 180, y - 5);
            e.HasMorePages = false;
        };
        using var dlg = new PrintPreviewDialog { Document = doc, Width = 900, Height = 750, StartPosition = FormStartPosition.CenterParent };
        dlg.ShowDialog();
    }
}

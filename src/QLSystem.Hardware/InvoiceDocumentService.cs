using System;
using System.Text;
using QLSystem.Core.Models;

namespace QLSystem.Hardware
{
    /// <summary>
    /// مسؤول عن تجهيز الفواتير الرسمية وبونات التوصيل بحجم A4 و A5 (Facture / Bon de Livraison)
    /// </summary>
    public static class InvoiceDocumentService
    {
        public static string GenerateHtmlDocument(Sale sale, StoreSettings settings, string documentTitle = "FACTURE")
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='fr'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine($"<title>{documentTitle} - {sale.InvoiceNumber}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 30px; color: #333; }");
            sb.AppendLine(".header { display: flex; justify-content: space-between; border-bottom: 2px solid #2563eb; padding-bottom: 20px; }");
            sb.AppendLine(".company-info h2 { margin: 0 0 5px 0; color: #1e3a8a; }");
            sb.AppendLine(".doc-info { text-align: right; }");
            sb.AppendLine(".doc-info h1 { margin: 0; color: #2563eb; }");
            sb.AppendLine(".client-box { margin: 25px 0; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 15px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            sb.AppendLine("th { background-color: #2563eb; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine("td { padding: 10px; border-bottom: 1px solid #e2e8f0; }");
            sb.AppendLine(".totals { margin-top: 25px; float: right; width: 350px; }");
            sb.AppendLine(".totals-row { display: flex; justify-content: space-between; padding: 8px 0; }");
            sb.AppendLine(".grand-total { font-weight: bold; font-size: 1.2em; border-top: 2px solid #1e3a8a; color: #1e3a8a; padding-top: 10px; }");
            sb.AppendLine(".footer { margin-top: 150px; text-align: center; color: #64748b; font-size: 0.9em; border-top: 1px solid #e2e8f0; padding-top: 15px; }");
            sb.AppendLine("@media print { body { margin: 0; } .no-print { display: none; } }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("  <div class='company-info'>");
            sb.AppendLine($"    <h2>{settings.StoreName}</h2>");
            if (!string.IsNullOrEmpty(settings.OwnerName)) sb.AppendLine($"    <p>Gérant: {settings.OwnerName}</p>");
            if (!string.IsNullOrEmpty(settings.Address)) sb.AppendLine($"    <p>Adresse: {settings.Address}</p>");
            if (!string.IsNullOrEmpty(settings.Phone1)) sb.AppendLine($"    <p>Tél: {settings.Phone1} / {settings.Phone2}</p>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='doc-info'>");
            sb.AppendLine($"    <h1>{documentTitle}</h1>");
            sb.AppendLine($"    <p><strong>N°:</strong> {sale.InvoiceNumber}</p>");
            sb.AppendLine($"    <p><strong>Date:</strong> {sale.CreatedAt:dd/MM/yyyy HH:mm}</p>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            // Client Info
            sb.AppendLine("<div class='client-box'>");
            sb.AppendLine($"  <strong>Client:</strong> {sale.CustomerName}<br>");
            sb.AppendLine($"  <strong>Mode de règlement:</strong> {sale.PaymentType}");
            sb.AppendLine("</div>");

            // Table
            sb.AppendLine("<table>");
            sb.AppendLine("  <thead>");
            sb.AppendLine("    <tr>");
            sb.AppendLine("      <th>Réf</th>");
            sb.AppendLine("      <th>Désignation de l'article</th>");
            sb.AppendLine("      <th style='text-align: center;'>Qté</th>");
            sb.AppendLine("      <th style='text-align: right;'>Prix Unitaire (DZD)</th>");
            sb.AppendLine("      <th style='text-align: right;'>Total (DZD)</th>");
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </thead>");
            sb.AppendLine("  <tbody>");

            foreach (var item in sale.Items)
            {
                sb.AppendLine("    <tr>");
                sb.AppendLine($"      <td>{item.ProductReference}</td>");
                sb.AppendLine($"      <td>{item.ProductName}</td>");
                sb.AppendLine($"      <td style='text-align: center;'>{item.Quantity}</td>");
                sb.AppendLine($"      <td style='text-align: right;'>{item.UnitPrice:N2}</td>");
                sb.AppendLine($"      <td style='text-align: right;'>{item.TotalPrice:N2}</td>");
                sb.AppendLine("    </tr>");
            }

            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");

            // Totals
            sb.AppendLine("<div class='totals'>");
            sb.AppendLine($"  <div class='totals-row'><span>Total Brut:</span><span>{sale.SubTotal:N2} DZD</span></div>");
            if (sale.Discount > 0)
            {
                sb.AppendLine($"  <div class='totals-row'><span>Remise:</span><span>-{sale.Discount:N2} DZD</span></div>");
            }
            sb.AppendLine($"  <div class='totals-row grand-total'><span>NET À PAYER:</span><span>{sale.TotalAmount:N2} DZD</span></div>");
            sb.AppendLine($"  <div class='totals-row'><span>Versement:</span><span>{sale.PaidAmount:N2} DZD</span></div>");
            if (sale.DebtAmount > 0)
            {
                sb.AppendLine($"  <div class='totals-row' style='color: #dc2626; font-weight: bold;'><span>Reste Crédit:</span><span>{sale.DebtAmount:N2} DZD</span></div>");
            }
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div style='clear: both;'></div>");
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine($"  <p>{settings.ReceiptFooter}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}

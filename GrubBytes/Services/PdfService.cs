using GrubBytes.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GrubBytes.Services
{
    public class PdfService
    {
        public byte[] GenerateReceipt(Order order)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // HEADER
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("GrubBytes")
                                    .FontSize(28).Bold().FontColor("#FF6B35");
                                c.Item().Text("Your neighborhood, your flavor.")
                                    .FontSize(10).FontColor("#888880");
                            });
                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text("ORDER RECEIPT")
                                    .FontSize(14).Bold().FontColor("#1A1A1A");
                                c.Item().Text($"#{order.Id}")
                                    .FontSize(20).Bold().FontColor("#FF6B35");
                            });
                        });
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#E0E0E0");
                    });

                    // CONTENT
                    page.Content().PaddingTop(20).Column(col =>
                    {
                        // Order info
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Order Details").FontSize(13).Bold();
                                c.Item().PaddingTop(4).Text($"Order ID: #{order.Id}");
                                c.Item().Text($"Date: {order.CreatedAt:dd MMM yyyy, HH:mm}");
                                c.Item().Text($"Status: {order.Status}");
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Customer").FontSize(13).Bold();
                                c.Item().PaddingTop(4).Text(order.User?.FullName ?? "—");
                                c.Item().Text(order.User?.Email ?? "—").FontColor("#888880");
                            });
                        });

                        col.Item().PaddingTop(20).Text("Items Ordered").FontSize(13).Bold();
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell().Background("#FF6B35").Padding(8)
                                    .Text("Item").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#FF6B35").Padding(8)
                                    .Text("Qty").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#FF6B35").Padding(8)
                                    .Text("Unit Price").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#FF6B35").Padding(8)
                                    .Text("Subtotal").FontColor("#FFFFFF").Bold();
                            });

                            // Table rows
                            var rowColor = false;
                            foreach (var item in order.OrderItems)
                            {
                                var bg = rowColor ? "#F9F9F9" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(8)
                                    .Text(item.MenuItem?.Title ?? "—");
                                table.Cell().Background(bg).Padding(8)
                                    .Text(item.Quantity.ToString());
                                table.Cell().Background(bg).Padding(8)
                                    .Text($"₺{item.UnitPrice:0.00}");
                                table.Cell().Background(bg).Padding(8)
                                    .Text($"₺{(item.UnitPrice * item.Quantity):0.00}");
                                rowColor = !rowColor;
                            }
                        });

                        // Total
                        col.Item().PaddingTop(16).AlignRight().Text(text =>
                        {
                            text.Span("Total Paid: ").FontSize(14).Bold();
                            text.Span($"₺{order.TotalAmount:0.00}")
                                .FontSize(16).Bold().FontColor("#FF6B35");
                        });

                        // Divider
                        col.Item().PaddingTop(30).LineHorizontal(1).LineColor("#E0E0E0");

                        // Thank you note
                        col.Item().PaddingTop(16).AlignCenter().Text(text =>
                        {
                            text.Span("Thank you for ordering with ")
                                .FontSize(11).FontColor("#888880");
                            text.Span("GrubBytes").FontSize(11)
                                .Bold().FontColor("#FF6B35");
                            text.Span("!").FontSize(11).FontColor("#888880");
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated on ")
                            .FontSize(9).FontColor("#AAAAAA");
                        text.Span(DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"))
                            .FontSize(9).FontColor("#AAAAAA");
                        text.Span(" · GrubBytes © 2026")
                            .FontSize(9).FontColor("#AAAAAA");
                    });
                });
            }).GeneratePdf();
        }

        public byte[] GenerateAgreement(Order order)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("GrubBytes")
                            .FontSize(28).Bold().FontColor("#FF6B35");
                        col.Item().Text("Order Agreement")
                            .FontSize(16).Bold().FontColor("#1A1A1A");
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#E0E0E0");
                    });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Item().Text($"Agreement for Order #{order.Id}")
                            .FontSize(13).Bold();
                        col.Item().PaddingTop(8).Text(
                            $"This agreement is made between GrubBytes and " +
                            $"{order.User?.FullName ?? "the customer"} " +
                            $"on {order.CreatedAt:dd MMM yyyy} for the following order:")
                            .FontColor("#444444");

                        col.Item().PaddingTop(16).Text("Order Summary").Bold();
                        foreach (var item in order.OrderItems)
                        {
                            col.Item().PaddingTop(4).Text(
                                $"• {item.MenuItem?.Title} × {item.Quantity} " +
                                $"— ₺{(item.UnitPrice * item.Quantity):0.00}");
                        }

                        col.Item().PaddingTop(8).Text($"Total Amount: ₺{order.TotalAmount:0.00}")
                            .Bold().FontColor("#FF6B35");

                        col.Item().PaddingTop(24).Text("Terms & Conditions").Bold();
                        col.Item().PaddingTop(8).Text(
                            "1. The customer agrees to pay the full amount stated above.\n" +
                            "2. GrubBytes agrees to deliver the order as described.\n" +
                            "3. Refunds are subject to GrubBytes refund policy.\n" +
                            "4. This document serves as proof of purchase.\n" +
                            "5. By placing this order, the customer accepts all terms.")
                            .FontColor("#444444");

                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Customer Signature").Bold();
                                c.Item().PaddingTop(30).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(4).Text(order.User?.FullName ?? "—")
                                    .FontColor("#888880");
                            });
                            row.ConstantItem(50);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("GrubBytes").Bold();
                                c.Item().PaddingTop(30).LineHorizontal(1).LineColor("#AAAAAA");
                                c.Item().PaddingTop(4).Text("Authorized Signature")
                                    .FontColor("#888880");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Order #{order.Id} · ")
                            .FontSize(9).FontColor("#AAAAAA");
                        text.Span(DateTime.UtcNow.ToString("dd MMM yyyy"))
                            .FontSize(9).FontColor("#AAAAAA");
                        text.Span(" · GrubBytes © 2026")
                            .FontSize(9).FontColor("#AAAAAA");
                    });
                });
            }).GeneratePdf();
        }
    }
}
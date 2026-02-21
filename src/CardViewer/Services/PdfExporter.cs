using CardViewer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CardViewer.Services;

public class PdfExporter
{
    public void ExportCards(List<SpeechCard> cards, string outputPath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            foreach (var card in cards)
            {
                container.Page(page =>
                {
                    page.Size(5, 3, Unit.Inch);
                    page.Margin(0.25f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        // Hierarchy — escalating size toward topic
                        foreach (var item in card.HierarchyItems)
                        {
                            var textItem = col.Item().PaddingLeft(item.Indent * 0.5f)
                                .Text(item.Text)
                                .FontSize((float)item.FontSize * 0.6f)
                                .FontColor(Colors.Grey.Medium);
                            if (item.FontWeight == "SemiBold")
                                textItem.SemiBold();
                        }

                        if (card.BreadcrumbPath.Count > 0)
                            col.Item().PaddingTop(4);

                        // Main topic — left-aligned, bold
                        col.Item().ExtendVertical().AlignLeft().AlignMiddle()
                            .Column(topicCol =>
                            {
                                topicCol.Item().Text(card.Topic)
                                    .FontSize(14).SemiBold();

                                // Bullet points (depth 3+ items)
                                foreach (var bullet in card.Bullets)
                                {
                                    topicCol.Item().PaddingLeft(8 + bullet.IndentLevel * 6)
                                        .PaddingTop(2)
                                        .Text($"\u2022 {bullet.Text}")
                                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                                }
                            });
                    });

                    page.Footer().AlignRight()
                        .Text($"{card.CardNumber} / {card.TotalCards}")
                        .FontSize(6).FontColor(Colors.Grey.Lighten1);
                });
            }
        }).GeneratePdf(outputPath);
    }
}

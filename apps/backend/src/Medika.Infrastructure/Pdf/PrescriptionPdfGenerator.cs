using Medika.Application.Common.Interfaces;
using Medika.Application.Medical.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Medika.Infrastructure.Pdf;

/// <summary>
/// Renders a printable ordonnance with the cabinet letterhead. Uses QuestPDF
/// (Community licence — set once at startup in <c>DependencyInjection</c>).
/// </summary>
public sealed class PrescriptionPdfGenerator : IPrescriptionPdfGenerator
{
    private static readonly string Teal = Colors.Teal.Darken2;
    private static readonly string Muted = Colors.Grey.Darken1;
    private static readonly string Line = Colors.Grey.Lighten2;

    public byte[] Generate(PrescriptionPdfModel model)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black).FontFamily(Fonts.Calibri));

                page.Header().Element(c => ComposeHeader(c, model.Doctor));
                page.Content().Element(c => ComposeContent(c, model));
                page.Footer().Element(c => ComposeFooter(c, model.Doctor));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, DoctorLetterhead d)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(d.FullName).FontSize(15).Bold().FontColor(Teal);
                    if (!string.IsNullOrWhiteSpace(d.Specialty))
                        left.Item().Text(d.Specialty!).FontSize(10.5f).FontColor(Muted);
                    if (!string.IsNullOrWhiteSpace(d.OrderNumber))
                        left.Item().Text($"N° d'ordre : {d.OrderNumber}").FontSize(9).FontColor(Muted);
                });

                row.ConstantItem(220).AlignRight().Column(right =>
                {
                    if (!string.IsNullOrWhiteSpace(d.CabinetName))
                        right.Item().Text(d.CabinetName!).FontSize(11).Bold();

                    var locality = string.Join(", ", new[] { d.CabinetCity, d.CabinetWilaya }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));

                    if (!string.IsNullOrWhiteSpace(d.CabinetAddress))
                        right.Item().Text(d.CabinetAddress!).FontSize(9).FontColor(Muted);
                    if (!string.IsNullOrWhiteSpace(locality))
                        right.Item().Text(locality).FontSize(9).FontColor(Muted);
                    if (!string.IsNullOrWhiteSpace(d.CabinetPhone))
                        right.Item().Text($"Tél : {d.CabinetPhone}").FontSize(9).FontColor(Muted);
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(Teal);
        });
    }

    private static void ComposeContent(IContainer container, PrescriptionPdfModel model)
    {
        container.PaddingVertical(18).Column(col =>
        {
            col.Spacing(14);

            // Patient + date row
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(p =>
                {
                    p.Item().Text("Patient").FontSize(8.5f).FontColor(Muted).Bold().LetterSpacing(0.05f);
                    p.Item().Text(model.Patient.FullName).FontSize(12).Bold();
                    p.Item().Text($"{model.Patient.Age} ans · {GenderLabel(model.Patient.Gender)}")
                        .FontSize(9.5f).FontColor(Muted);
                });

                row.ConstantItem(160).AlignRight().Column(p =>
                {
                    p.Item().Text("Date").FontSize(8.5f).FontColor(Muted).Bold().LetterSpacing(0.05f);
                    p.Item().Text(model.Date.ToLocalTime().ToString("dd/MM/yyyy")).FontSize(12);
                });
            });

            // Title
            col.Item().PaddingTop(4).Text("ORDONNANCE").FontSize(13).Bold().FontColor(Teal).AlignCenter();

            // Prescription lines
            col.Item().PaddingTop(2).Column(lines =>
            {
                lines.Spacing(10);
                var i = 1;
                foreach (var line in model.Lines)
                {
                    lines.Item().Row(row =>
                    {
                        row.ConstantItem(22).Text($"{i}.").FontSize(11).Bold().FontColor(Teal);
                        row.RelativeItem().Column(med =>
                        {
                            med.Item().Text(line.Medication).FontSize(11.5f).Bold();

                            var posology = string.Join("  ·  ", new[]
                            {
                                line.Dosage,
                                line.Frequency,
                                line.Duration
                            }.Where(s => !string.IsNullOrWhiteSpace(s)));

                            if (!string.IsNullOrWhiteSpace(posology))
                                med.Item().Text(posology).FontSize(10).FontColor(Colors.Grey.Darken2);

                            if (line.Quantity > 0)
                                med.Item().Text($"Quantité : {line.Quantity} boîte{(line.Quantity > 1 ? "s" : "")}")
                                    .FontSize(9).FontColor(Muted);
                        });
                    });

                    lines.Item().LineHorizontal(0.5f).LineColor(Line);
                    i++;
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container, DoctorLetterhead d)
    {
        container.Column(col =>
        {
            col.Item().AlignRight().PaddingTop(24).Width(200).Column(sig =>
            {
                sig.Item().Text("Signature et cachet").FontSize(9).FontColor(Muted).AlignCenter();
                sig.Item().PaddingTop(36).LineHorizontal(0.75f).LineColor(Colors.Grey.Medium);
                sig.Item().PaddingTop(4).Text(d.FullName).FontSize(9.5f).AlignCenter();
            });

            col.Item().PaddingTop(8).Text(t =>
            {
                t.Span("Document généré par Medika").FontSize(7.5f).FontColor(Colors.Grey.Lighten1);
            });
        });
    }

    private static string GenderLabel(string gender) => gender?.ToUpperInvariant() switch
    {
        "M" => "Homme",
        "F" => "Femme",
        _ => gender ?? ""
    };
}

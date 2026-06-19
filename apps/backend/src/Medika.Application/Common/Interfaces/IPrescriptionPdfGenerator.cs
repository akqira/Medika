using Medika.Application.Medical.Pdf;

namespace Medika.Application.Common.Interfaces;

/// <summary>
/// Server-side ordonnance PDF rendering. Implementation lives in Infrastructure
/// (QuestPDF). Letterhead is configurable per cabinet from day one — see ROADMAP
/// architecture decision #2.
/// </summary>
public interface IPrescriptionPdfGenerator
{
    byte[] Generate(PrescriptionPdfModel model);
}

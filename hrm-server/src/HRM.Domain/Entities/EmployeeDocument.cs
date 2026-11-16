using HRM.Domain.Common;

namespace HRM.Domain.Entities;

/// <summary>
/// An uploaded CV (PDF). Its own aggregate, not a child of Employee, so loading an employee never
/// loads megabytes of file content. One CV per employee; uploading again replaces it.
/// </summary>
public class EmployeeDocument : AuditableEntity
{
    public const int MaxSizeBytes = 5 * 1024 * 1024;
    public const string PdfContentType = "application/pdf";
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    private EmployeeDocument() { } // EF Core

    public Guid EmployeeId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = PdfContentType;
    public int SizeBytes { get; private set; }
    public byte[] Content { get; private set; } = [];

    /// <summary>
    /// Checks the bytes, not just the name: a file is accepted only if it starts with the PDF
    /// signature. The stored name is reduced to a plain file name (no folders) for safe download.
    /// </summary>
    public static EmployeeDocument CreateCv(Guid employeeId, string fileName, byte[] content)
    {
        var document = new EmployeeDocument { EmployeeId = employeeId };
        document.Replace(fileName, content);
        return document;
    }

    /// <summary>A new upload for the same employee replaces the file in place (same checks).</summary>
    public void Replace(string fileName, byte[] content)
    {
        if (content.Length == 0)
            throw new DomainException("The file is empty.");
        if (content.Length > MaxSizeBytes)
            throw new DomainException($"The CV must be at most {MaxSizeBytes / 1024 / 1024} MB.");
        if (!content.AsSpan().StartsWith(PdfSignature))
            throw new DomainException("The CV must be a PDF file.");

        FileName = SafeFileName(fileName);
        ContentType = PdfContentType;
        SizeBytes = content.Length;
        Content = content;
    }

    private static string SafeFileName(string fileName)
    {
        // Strip any path (both separators, whatever the server OS) and characters unsafe in headers.
        var name = fileName.Replace('\\', '/').Split('/')[^1];
        name = new string(name.Where(c => !char.IsControl(c) && c is not ('"' or ';')).ToArray()).Trim();
        if (name.Length == 0)
            name = "cv.pdf";
        if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            name += ".pdf";
        return name.Length > 200 ? name[..196] + ".pdf" : name;
    }
}

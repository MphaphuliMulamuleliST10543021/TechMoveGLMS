using Microsoft.AspNetCore.Http;
using TechMoveGLMS.Web.Services;

namespace TechMoveGLMS.Tests;

public class ContractPdfServiceTests
{
    private static FormFile CreateFile(
        string fileName,
        byte[] content)
    {
        var stream = new MemoryStream(content);

        return new FormFile(
            stream,
            0,
            content.Length,
            "file",
            fileName);
    }

    [Fact]
    public void IsValidPdf_WithPdfSignatureAndPdfExtension_ReturnsTrue()
    {
        var content = System.Text.Encoding.ASCII.GetBytes(
            "%PDF-1.7\nTest PDF content");

        var file = CreateFile("contract.pdf", content);

        Assert.True(ContractPdfService.IsValidPdf(file));
    }

    [Fact]
    public void IsValidPdf_WithWrongExtension_ReturnsFalse()
    {
        var content = System.Text.Encoding.ASCII.GetBytes(
            "%PDF-1.7\nTest PDF content");

        var file = CreateFile("contract.txt", content);

        Assert.False(ContractPdfService.IsValidPdf(file));
    }

    [Fact]
    public void IsValidPdf_WithInvalidSignature_ReturnsFalse()
    {
        var content = System.Text.Encoding.ASCII.GetBytes(
            "This is not a PDF file");

        var file = CreateFile("contract.pdf", content);

        Assert.False(ContractPdfService.IsValidPdf(file));
    }

    [Fact]
    public void IsValidPdf_WithEmptyFile_ReturnsFalse()
    {
        var file = CreateFile("contract.pdf", Array.Empty<byte>());

        Assert.False(ContractPdfService.IsValidPdf(file));
    }

    [Fact]
    public void IsValidPdf_WithFileLargerThanFiveMegabytes_ReturnsFalse()
    {
        var content = new byte[
            ContractPdfService.MaxFileSize + 1];

        var header = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7");
        Array.Copy(header, content, header.Length);

        var file = CreateFile("contract.pdf", content);

        Assert.False(ContractPdfService.IsValidPdf(file));
    }
}
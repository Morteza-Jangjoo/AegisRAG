using AegisRAG.Domain.Entities;
using AegisRAG.Domain.Enums;

namespace AegisRAG.UnitTests.Domain;

public class DocumentTests
{
    [Fact]
    public void New_document_should_have_uploaded_status()
    {
        // Arrange
        var document = new Document(
            "test.pdf",
            "application/pdf",
            1024,
            "/documents/test.pdf");

        // Assert
        Assert.Equal(
            DocumentStatus.Uploaded,
            document.Status);
    }

    [Fact]
    public void Document_should_be_marked_as_processing()
    {
        // Arrange
        var document = new Document(
            "test.pdf",
            "application/pdf",
            1024,
            "/documents/test.pdf");

        // Act
        document.MarkAsProcessing();

        // Assert
        Assert.Equal(
            DocumentStatus.Processing,
            document.Status);
    }

    [Fact]
    public void Document_should_be_marked_as_completed()
    {
        // Arrange
        var document = new Document(
            "test.pdf",
            "application/pdf",
            1024,
            "/documents/test.pdf");

        // Act
        document.MarkAsProcessing();
        document.MarkAsCompleted();

        // Assert
        Assert.Equal(
            DocumentStatus.Completed,
            document.Status);

        Assert.NotNull(document.ProcessedAtUtc);
    }

    [Fact]
    public void Document_should_be_able_to_add_chunks()
    {
        // Arrange
        var document = new Document(
            "test.pdf",
            "application/pdf",
            1024,
            "/documents/test.pdf");

        // Act
        document.AddChunk(
            "Dependency Injection is...",
            0,
            1);

        // Assert
        Assert.Single(document.Chunks);

        var chunk = document.Chunks.First();

        Assert.Equal(
            "Dependency Injection is...",
            chunk.Content);

        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(1, chunk.PageNumber);
    }
}
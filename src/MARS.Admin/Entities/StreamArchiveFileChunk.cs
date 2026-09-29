using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.Admin.Entities;

public class StreamArchiveFileChunk
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid FileId { get; set; }
    public int ChunkNumber { get; set; }
    public int TotalChunks { get; set; }
    public string ChunkFileName { get; set; } = null!;
    public long ChunkSize { get; set; }
    public long OffsetInOriginalFile { get; set; }
    public DateTime? UploadedAt { get; set; }
    public long? TelegramMessageId { get; set; }
    public StreamArchiveChunkStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(FileId))]
    public virtual StreamArchiveFile File { get; set; } = null!;
}

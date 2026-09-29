using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MARS.Admin.Entities;

public class StreamArchiveFile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid ConfigId { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string ProcessedFileName { get; set; } = null!;
    public string OriginalFilePath { get; set; } = null!;
    public long OriginalFileSize { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    public StreamArchiveFileStatus Status { get; set; }
    public int ChunksCount { get; set; }
    public string? ErrorMessage { get; set; }
    public long? TelegramMessageId { get; set; }

    public virtual ICollection<StreamArchiveFileChunk> Chunks { get; set; } = [];

    [ForeignKey(nameof(ConfigId))]
    public virtual StreamArchiveConfig Config { get; set; } = null!;
}

using System.Diagnostics.CodeAnalysis;
namespace MARS.MediaStorage.Domain.Entities;

public enum MediaDownloadArtifactKind
{
    Unknown = 0,
    Video = 1,
    Audio = 2,
}
using MARS.Shared.Models.Media;
using Microsoft.AspNetCore.Http;

namespace MARS.MediaStorage.Services.Media;

public interface IMediaFileStorageService
{
    Task<MediaFileInfo> SaveFileAsync(IFormFile file, string? targetRelativePathHint = null);
    Task DeleteFileAsync(string relativePath);
    Task CopyToDevCopiesAsync(string relativePath);
}

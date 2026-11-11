using MARS.MediaStorage.Application.MemoryStorage;
using Microsoft.AspNetCore.Mvc;

namespace MARS.MediaStorage.Api.Rest.Controllers;

[Route("memory")]
public class MemoryController : Controller
{
    [HttpGet]
    [Route("{*escapedFileName:required}")]
    public async Task Index(string escapedFileName)
    {
        var context = ControllerContext;
        var fileName = Uri.UnescapeDataString(escapedFileName);
        var isFound = MemoryStorage.FileExists(fileName);

        if (isFound)
        {
            (MemoryStream stream, var contentType) =
                await MemoryStorage.GetFileStreamWithContentTypeAsync(fileName);
            var result = new FileStreamResult(stream, contentType)
            {
                EnableRangeProcessing = true,
                LastModified = DateTime.Now,
            };

            await result.ExecuteResultAsync(context);
            await MemoryStorage.DeleteFileAsync(fileName);
            return;
        }

        var result2 = new BadRequestResult();
        await result2.ExecuteResultAsync(context);
    }
}

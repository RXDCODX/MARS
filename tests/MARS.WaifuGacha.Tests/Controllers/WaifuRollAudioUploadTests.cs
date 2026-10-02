using System.Text;
using MARS.Shared.Models;
using MARS.WaifuGacha.Configuration;
using MARS.WaifuGacha.Controllers;
using MARS.WaifuGacha.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Tests.Controllers;

/// <summary>
/// Загрузка звука для ролла вайфу.
///
/// Звук слышат все зрители, поэтому проверяется отсев: пустой файл, пустое имя и
/// неподдерживаемый формат не попадают в базу, а принятый файл читается целиком и
/// сохраняется.
/// </summary>
public class WaifuRollAudioUploadTests
{
    private readonly WaifuTestDbContextFactory _factory = new();

    [Fact]
    public async Task AudioIsStoredWithItsName()
    {
        var result = await Upload(Form("аяка.mp3", "звук"), "Аяка");

        Assert.True(result.Success);
        Assert.Equal("Аяка", result.Result!.Name);
        Assert.Equal(".mp3", result.Result.FileExtension);
    }

    /// <summary>
    /// Содержимое сохраняется целиком: обрезанный файл проигрывался бы с рывком.
    /// </summary>
    [Fact]
    public async Task AudioBytesAreStored()
    {
        await Upload(Form("аяка.mp3", "звук"), "Аяка");

        await using var db = await _factory.CreateDbContextAsync(Token);
        var audio = await db.WaifuRollAudios.SingleAsync(Token);

        Assert.Equal(Encoding.UTF8.GetBytes("звук"), audio.AudioData);
    }

    /// <summary>
    /// Без имени звук не сохраняется: в ленте ролла он был бы без подписи.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyNameIsRejected(string? name)
    {
        var result = await Upload(Form("аяка.mp3", "звук"), name!);

        Assert.False(result.Success);
        await AssertNothingStored();
    }

    /// <summary>
    /// Неподдерживаемый формат отвергается: иначе в очередь попал бы файл, который
    /// плеер не сможет воспроизвести.
    /// </summary>
    [Theory]
    [InlineData("аяка.txt")]
    [InlineData("аяка.exe")]
    [InlineData("аяка")]
    public async Task UnsupportedFormatIsRejected(string fileName)
    {
        var result = await Upload(Form(fileName, "звук"), "Аяка");

        Assert.False(result.Success);
        await AssertNothingStored();
    }

    /// <summary>
    /// Пустой файл не сохраняется: имя есть, а звука нет.
    /// </summary>
    [Fact]
    public async Task EmptyFileIsRejected()
    {
        var result = await Upload(Form("аяка.mp3", ""), "Аяка");

        Assert.False(result.Success);
        await AssertNothingStored();
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<OperationResult<WaifuRollAudioDto?>> Upload(IFormFile file, string name)
    {
        var controller = new WaifuRollController(
            _factory,
            Options.Create(new ShikimoriSiteOptions { ShikimoriSite = "https://shikimori.one" }),
            NullLogger<WaifuRollController>.Instance
        );

        var action = await controller.UploadAudio(file, name, Token);
        var objectResult = Assert.IsType<OkObjectResult>(action.Result);

        return Assert.IsType<OperationResult<WaifuRollAudioDto?>>(objectResult.Value);
    }

    /// <summary>
    /// Отказ всегда приходит кодом 200 с признаком в теле: клиент читает успех из
    /// тела, а не из кода ответа.
    /// </summary>
    private async Task AssertNothingStored()
    {
        await using var db = await _factory.CreateDbContextAsync(Token);

        Assert.Empty(await db.WaifuRollAudios.ToListAsync(Token));
    }

    private static FormFile Form(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);

        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "audio", fileName);
    }
}

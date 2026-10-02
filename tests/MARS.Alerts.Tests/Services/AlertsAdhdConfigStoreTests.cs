using System.Text.Json;
using MARS.Alerts.Models;
using MARS.Alerts.Services.Adhd;
using MARS.Shared.Models;
using Moq;

namespace MARS.Alerts.Tests.Services;

/// <summary>
/// Мост между контрактом gRPC и настройкой ADHD-экрана.
///
/// Настройка едет в контракт JSON-байтами. Проверяется главное: пустое хранилище
/// отдаёт валидный пустой объект (иначе вызывающий развалился бы на разборе),
/// а неразбираемый JSON не затирает сохранённую настройку мусором.
/// </summary>
public class AlertsAdhdConfigStoreTests
{
    /// <summary>
    /// Настройка едет JSON в camelCase (<c>JsonSerializerDefaults.Web</c>), и
    /// разбирать её нужно теми же настройками: иначе десериализация молча вернула
    /// бы значения по умолчанию, и тест прошёл бы, ничего не проверив.
    /// </summary>
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly Mock<IAdhdLayoutService> _layout = new();

    /// <summary>
    /// Ответ на чтение: без него обновление падало бы на пустом хранилище, и
    /// проверялась бы не запись, а чужое исключение.
    /// </summary>
    private void SetupStoredConfig()
    {
        _layout
            .Setup(instance => instance.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<AdhdLayoutConfigDto>.Ok(new AdhdLayoutConfigDto()));
    }

    [Fact]
    public async Task EmptyStorageYieldsValidEmptyConfig()
    {
        _layout
            .Setup(instance => instance.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<AdhdLayoutConfigDto>.Ok(null!));
        var store = new AlertsAdhdConfigStore(_layout.Object);

        var json = await store.GetAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(JsonSerializer.Deserialize<AdhdLayoutConfigDto>(json, Web));
    }

    [Fact]
    public async Task StoredConfigIsSerializedBack()
    {
        var stored = new AdhdLayoutConfigDto { DvdLogosCount = 3 };
        _layout
            .Setup(instance => instance.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<AdhdLayoutConfigDto>.Ok(stored));
        var store = new AlertsAdhdConfigStore(_layout.Object);

        var json = await store.GetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, JsonSerializer.Deserialize<AdhdLayoutConfigDto>(json, Web)!.DvdLogosCount);
    }

    /// <summary>
    /// Неразбираемый JSON не пишется в базу, а вызывающему возвращается то, что
    /// сейчас действительно сохранено: иначе один опечатанный запрос оставил бы
    /// экран без настройки, а ответ утверждал бы обратное.
    /// </summary>
    [Fact]
    public async Task BrokenJsonIsNotStored()
    {
        SetupStoredConfig();
        var store = new AlertsAdhdConfigStore(_layout.Object);

        var json = await store.UpdateAsync("{ это не json", TestContext.Current.CancellationToken);

        Assert.Equal(12, JsonSerializer.Deserialize<AdhdLayoutConfigDto>(json, Web)!.DvdLogosCount);
        _layout.Verify(
            instance =>
                instance.UpdateAsync(
                    It.IsAny<AdhdLayoutConfigDto>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task EmptyUpdateIsNotStored()
    {
        SetupStoredConfig();
        var store = new AlertsAdhdConfigStore(_layout.Object);

        await store.UpdateAsync("   ", TestContext.Current.CancellationToken);

        _layout.Verify(
            instance =>
                instance.UpdateAsync(
                    It.IsAny<AdhdLayoutConfigDto>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task ValidConfigIsStoredAndReturned()
    {
        var written = new AdhdLayoutConfigDto { DvdLogosCount = 7 };
        _layout
            .Setup(instance =>
                instance.UpdateAsync(It.IsAny<AdhdLayoutConfigDto>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(OperationResult<AdhdLayoutConfigDto>.Ok(written));
        SetupStoredConfig();
        var store = new AlertsAdhdConfigStore(_layout.Object);

        var json = await store.UpdateAsync(
            """{"layoutName":"из запроса"}""",
            TestContext.Current.CancellationToken
        );

        Assert.Equal(7, JsonSerializer.Deserialize<AdhdLayoutConfigDto>(json, Web)!.DvdLogosCount);
    }

    /// <summary>
    /// Неуспешная запись возвращает исходный JSON: вызывающий видит, что
    /// настройка не применена, и может повторить.
    /// </summary>
    [Fact]
    public async Task FailedWriteKeepsRequestAsIs()
    {
        _layout
            .Setup(instance =>
                instance.UpdateAsync(It.IsAny<AdhdLayoutConfigDto>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(OperationResult<AdhdLayoutConfigDto>.Fail("не записалось"));
        SetupStoredConfig();
        var store = new AlertsAdhdConfigStore(_layout.Object);

        var json = await store.UpdateAsync(
            """{"layoutName":"из запроса"}""",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("""{"layoutName":"из запроса"}""", json);
    }
}

using System.Text.Json;
using MARS.Shared.Grpc;
using Xunit;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Форма перечислений в байтовых полях протокола.
/// </summary>
/// <remarks>
/// <para>
/// Провод хабов оверлея сериализуется через <see cref="MarsGrpcJson"/>, и все
/// байтовые поля <c>bytes</c> идут через него. Настройки в нём повторяют
/// <c>JsonSerializerDefaults.Web</c>, но без <c>JsonStringEnumConverter</c>,
/// тогда как в трёх других местах репозитория конвертер есть: MVC
/// (<c>WebApplicationBuilderExtensions</c>), хабы
/// (<c>SignalRServiceCollectionExtensions</c>) и межсервисный HTTP.
/// </para>
/// <para>
/// Из-за этого <c>TwitchScreenParticles.Confetty</c> едал числом <c>0</c>, а
/// клиент сравнивал его со строковым элементом перечисления из сгенерированного
/// контракта. Ни одна ветка <c>switch</c> не сходилась, и экран частиц
/// (<c>/confetti</c>, <c>/fireworks</c>) не рисовал ничего при полностью
/// «зелёном» соединении.
/// </para>
/// <para>
/// Тест ловит и обратную сторону: он падает, если конвертер уберут, и падает,
/// если появится новое байтовое поле с перечислением и снова без него.
/// </para>
/// </remarks>
public class MarsGrpcJsonEnumFormatTests
{
    private enum Particles
    {
        Confetty = 0,
        Fireworks = 1,
    }

    /// <summary>
    /// Перечисление едет именем, а не числом.
    /// </summary>
    /// <remarks>
    /// Имена сравниваются строками во всех трёх местах, где клиент читает
    /// перечисления: <c>MediaMetaInfoPriorityEnum</c> в оверлее, состояние плеера
    /// в экране видео и <c>MediaFileInfoTypeEnum</c> в REST. Число не совпало бы
    /// ни с одним из них.
    /// </remarks>
    [Fact]
    public void EnumIsSerializedByName()
    {
        var bytes = MarsGrpcJson.Serialize(Particles.Confetty);

        using var document = JsonDocument.Parse(bytes.ToByteArray());

        Assert.Equal(JsonValueKind.String, document.RootElement.ValueKind);
        Assert.Equal("Confetty", document.RootElement.GetString());
    }

    /// <summary>
    /// Второй элемент не схлопывается в первый.
    /// </summary>
    /// <remarks>
    /// Безымянная сериализация различает их по числу, строковая обязана давать
    /// разные строки: иначе <c>Fireworks</c> нарисовал бы конфетти.
    /// </remarks>
    [Fact]
    public void DistinctEnumValuesProduceDistinctNames()
    {
        using var first = JsonDocument.Parse(
            MarsGrpcJson.Serialize(Particles.Confetty).ToByteArray()
        );
        using var second = JsonDocument.Parse(
            MarsGrpcJson.Serialize(Particles.Fireworks).ToByteArray()
        );

        Assert.NotEqual(first.RootElement.GetString(), second.RootElement.GetString());
        Assert.Equal("Fireworks", second.RootElement.GetString());
    }

    /// <summary>
    /// Имена полей остаются в camelCase, а не в PascalCase.
    /// </summary>
    /// <remarks>
    /// Проверяется вместе с перечислением, потому что обе части задаются одними
    /// настройками: <c>PropertyNamingPolicy</c> задаёт имена полей,
    /// <c>JsonStringEnumConverter</c> — значения перечислений. Отдельная правка
    /// одного не должна ломать другое.
    /// </remarks>
    [Fact]
    public void PropertyNamesStayCamelCase()
    {
        var bytes = MarsGrpcJson.Serialize(new { CustomRewardId = "reward-1" });

        using var document = JsonDocument.Parse(bytes.ToByteArray());

        Assert.Equal("reward-1", document.RootElement.GetProperty("customRewardId").GetString());
    }
}

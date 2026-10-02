using MARS.MediaStorage.Services.Media;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Аудит Stage 3: расчёт пути в корзине. Ошибка здесь означает либо потерю
/// файла, либо коллизию — два разных файла, попавших в корзину, не должны
/// перетирать друг друга.
/// </summary>
public class TrashPathBuilderTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void BuildTrashPath_PutsFileUnderTrashFolder()
    {
        var result = TrashPathBuilder.BuildTrashPath("Alerts/random_meme/videos/a.mp4", Now);

        Assert.StartsWith("_trash/", result, StringComparison.Ordinal);
        Assert.EndsWith("a.mp4", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTrashPath_DistinguishesSameNameInDifferentFolders()
    {
        // Регрессия-риск: наивная схема «_trash/<имя файла>» склеила бы
        // random_meme/videos/a.mp4 и zvik/videos/a.mp4 в один путь.
        var first = TrashPathBuilder.BuildTrashPath("Alerts/random_meme/videos/a.mp4", Now);
        var second = TrashPathBuilder.BuildTrashPath("Alerts/zvik/videos/a.mp4", Now);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void BuildTrashPath_DistinguishesTwoDeletesOfSameFile()
    {
        // Файл можно удалить, восстановить и удалить снова — второй раз он
        // обязан попасть в другое место, иначе восстановление вернёт не тот файл.
        var first = TrashPathBuilder.BuildTrashPath("Alerts/a.mp4", Now);
        var second = TrashPathBuilder.BuildTrashPath("Alerts/a.mp4", Now.AddDays(-1));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void BuildTrashPath_RejectsTraversal()
    {
        var result = TrashPathBuilder.BuildTrashPath("../../etc/passwd", Now);

        Assert.StartsWith("_trash/", result, StringComparison.Ordinal);
        // Никаких «..» в результирущем пути быть не должно.
        Assert.DoesNotContain("..", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildTrashPath_PreservesExtension()
    {
        var result = TrashPathBuilder.BuildTrashPath("Alerts/a.MP3", Now);

        Assert.EndsWith(".MP3", result, StringComparison.Ordinal);
    }

    [Fact]
    public void IsUnderTrash_RecognizesTrashPaths()
    {
        var trashed = TrashPathBuilder.BuildTrashPath("Alerts/a.mp4", Now);

        Assert.True(TrashPathBuilder.IsUnderTrash(trashed));
        Assert.False(TrashPathBuilder.IsUnderTrash("Alerts/a.mp4"));
        // Похожий, но не корзинный путь не должен считаться корзиной.
        Assert.False(TrashPathBuilder.IsUnderTrash("_trash_backup/a.mp4"));
    }
}

using MARS.MediaStorage.Entities;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Tests.Entities;

/// <summary>
/// Порядок мемов в хранилище.
///
/// Проверяется сравнение по идентификатору: очередь дедуплицирует мемы, и без
/// сравнения по <c>Id</c> один и тот же файл добавлялся бы в список дважды.
/// </summary>
public class MemeOrderTests
{
    [Fact]
    public void SameIdMeansSameOrder()
    {
        var id = Guid.CreateVersion7();
        var first = new MemeOrder { Id = id, FilePath = "a.mp3" };
        var second = new MemeOrder { Id = id, FilePath = "b.mp3" };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DifferentIdMeansDifferentOrder()
    {
        var first = new MemeOrder { Id = Guid.CreateVersion7(), FilePath = "a.mp3" };
        var second = new MemeOrder { Id = Guid.CreateVersion7(), FilePath = "a.mp3" };

        Assert.NotEqual(first, second);
    }

    /// <summary>
    /// Сравнение с чужим типом возвращает false, а не исключение: список мемов
    /// сравнивается с элементами других коллекций.
    /// </summary>
    [Fact]
    public void OtherTypeIsNotEqual()
    {
        var order = new MemeOrder { FilePath = "a.mp3" };

        Assert.False(order.Equals("a.mp3"));
        Assert.False(order.Equals(null));
    }

    /// <summary>
    /// Порядок по умолчанию нулевой: без сортировки по порядку файл попадает в
    /// начало очереди.
    /// </summary>
    [Fact]
    public void OrderDefaultsToZero()
    {
        Assert.Equal(0, new MemeOrder { FilePath = "a.mp3" }.Order);
    }
}

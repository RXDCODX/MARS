using MARS.MediaStorage.DataBaseContext;
using MARS.MediaStorage.Entities;
using MARS.MediaStorage.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Рандомные мемы поверх настоящего контекста SQLite в памяти.
///
/// Проверяются инварианты, на которых держится очередь показа: номера позиций
/// идут подряд и без дублей, тип с заказами не удаляется, а новое добавление
/// встаёт в конец, а не в начало очереди.
/// </summary>
public class RandomMemeServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContextFactory _factory;
    private readonly RandomMemeService _service;

    public RandomMemeServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _factory = new TestDbContextFactory(
            new DbContextOptionsBuilder<MediaStorageDbContext>().UseSqlite(_connection).Options
        );
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();

        _service = new RandomMemeService(_factory, NullLogger<RandomMemeService>.Instance);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task MemeTypeIsCreatedAndRead()
    {
        var created = await _service.CreateMemeTypeAsync(
            new MemeType { Name = "коты", FolderPath = "Alerts/random_meme/cats" },
            TestContext.Current.CancellationToken
        );

        Assert.True(created.Id > 0);
        var stored = await _service.GetMemeTypeByIdAsync(
            created.Id,
            TestContext.Current.CancellationToken
        );
        Assert.NotNull(stored);
        Assert.Equal("коты", stored!.Name);
    }

    [Fact]
    public async Task AllMemeTypesAreListed()
    {
        await CreateTypeAsync("коты");
        await CreateTypeAsync("собаки");

        var types = await _service.GetAllMemeTypesAsync(TestContext.Current.CancellationToken);

        Assert.Contains(types, type => type.Name == "коты");
        Assert.Contains(types, type => type.Name == "собаки");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveIdFindsNoType(int id)
    {
        Assert.Null(await _service.GetMemeTypeByIdAsync(id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task IncompleteMemeTypeIsNotCreated()
    {
        var noName = await _service.CreateMemeTypeAsync(
            new MemeType { Name = "  ", FolderPath = "путь" },
            TestContext.Current.CancellationToken
        );
        var noPath = await _service.CreateMemeTypeAsync(
            new MemeType { Name = "коты", FolderPath = string.Empty },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(string.Empty, noName.Name);
        Assert.Equal(string.Empty, noPath.FolderPath);
        Assert.DoesNotContain(
            await _service.GetAllMemeTypesAsync(TestContext.Current.CancellationToken),
            type => string.IsNullOrWhiteSpace(type.Name)
        );
    }

    [Fact]
    public async Task MemeTypeIsUpdated()
    {
        var created = await CreateTypeAsync("коты");

        var updated = await _service.UpdateMemeTypeAsync(
            new MemeType
            {
                Id = created.Id,
                Name = "кошки",
                FolderPath = "Alerts/random_meme/cats2",
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal("кошки", updated.Name);
        var reloaded = await _service.GetMemeTypeByIdAsync(
            created.Id,
            TestContext.Current.CancellationToken
        );
        Assert.Equal("кошки", reloaded!.Name);
    }

    [Fact]
    public async Task MemeTypeIsDeletedWhenUnused()
    {
        var created = await CreateTypeAsync("коты");

        Assert.True(
            await _service.DeleteMemeTypeAsync(created.Id, TestContext.Current.CancellationToken)
        );
        Assert.DoesNotContain(
            await _service.GetAllMemeTypesAsync(TestContext.Current.CancellationToken),
            type => type.Name == "коты"
        );
    }

    /// <summary>
    /// Тип с заказами не удаляется: иначе очередь осталась бы с битыми ссылками,
    /// и следующий показ упал бы с «тип не найден».
    /// </summary>
    [Fact]
    public async Task MemeTypeWithOrdersIsKept()
    {
        var created = await CreateTypeAsync("коты");
        await CreateOrderAsync(created.Id, "Alerts/random_meme/cats/1.mp4");

        Assert.False(
            await _service.DeleteMemeTypeAsync(created.Id, TestContext.Current.CancellationToken)
        );
        Assert.Contains(
            await _service.GetAllMemeTypesAsync(TestContext.Current.CancellationToken),
            type => type.Id == created.Id
        );
    }

    [Fact]
    public async Task UnknownMemeTypeIsNotDeleted()
    {
        Assert.False(
            await _service.DeleteMemeTypeAsync(999, TestContext.Current.CancellationToken)
        );
        Assert.False(await _service.DeleteMemeTypeAsync(0, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OrdersAreAppendedWithGrowingNumbers()
    {
        var type = await CreateTypeAsync("коты");

        var first = await CreateOrderAsync(type.Id, "Alerts/random_meme/cats/1.mp4");
        var second = await CreateOrderAsync(type.Id, "Alerts/random_meme/cats/2.mp4");

        Assert.Equal(1, first.Order);
        Assert.Equal(2, second.Order);
    }

    [Fact]
    public async Task IncompleteOrderIsNotCreated()
    {
        var type = await CreateTypeAsync("коты");

        var noPath = await _service.CreateMemeOrderAsync(
            new MemeOrder { FilePath = "  ", MemeTypeId = type.Id },
            TestContext.Current.CancellationToken
        );
        var noType = await _service.CreateMemeOrderAsync(
            new MemeOrder { FilePath = "файл.mp4", MemeTypeId = 0 },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(string.Empty, noPath.FilePath);
        Assert.Equal(string.Empty, noType.FilePath);
        Assert.DoesNotContain(
            await _service.GetAllMemeOrdersAsync(TestContext.Current.CancellationToken),
            order => order.FilePath is "" or "  "
        );
    }

    [Fact]
    public async Task OrdersAreListedByTypeInOrder()
    {
        var type = await CreateTypeAsync("коты");
        await CreateOrderAsync(type.Id, "первый.mp4");
        await CreateOrderAsync(type.Id, "второй.mp4");

        var orders = (
            await _service.GetMemeOrdersByTypeAsync(type.Id, TestContext.Current.CancellationToken)
        ).ToArray();

        Assert.Equal(["первый.mp4", "второй.mp4"], orders.Select(order => order.FilePath));
        Assert.All(orders, order => Assert.NotNull(order.Type));
    }

    [Fact]
    public async Task NonPositiveTypeIdReturnsNoOrders()
    {
        Assert.Empty(
            await _service.GetMemeOrdersByTypeAsync(0, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task OrderIsFoundById()
    {
        var type = await CreateTypeAsync("коты");
        var order = await CreateOrderAsync(type.Id, "кот.mp4");

        var found = await _service.GetMemeOrderByIdAsync(
            order.Id,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(found);
        Assert.Equal("кот.mp4", found!.FilePath);
    }

    [Fact]
    public async Task EmptyGuidFindsNoOrder()
    {
        Assert.Null(
            await _service.GetMemeOrderByIdAsync(Guid.Empty, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task OrderIsUpdated()
    {
        var type = await CreateTypeAsync("коты");
        var order = await CreateOrderAsync(type.Id, "кот.mp4");

        var updated = await _service.UpdateMemeOrderAsync(
            new MemeOrder
            {
                Id = order.Id,
                FilePath = "кот2.mp4",
                MemeTypeId = type.Id,
                Order = 5,
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal("кот2.mp4", updated.FilePath);
        Assert.Equal(5, updated.Order);
    }

    [Fact]
    public async Task UnknownOrderIsNotUpdated()
    {
        var updated = await _service.UpdateMemeOrderAsync(
            new MemeOrder
            {
                Id = Guid.NewGuid(),
                FilePath = "нет.mp4",
                MemeTypeId = 1,
                Order = 1,
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(string.Empty, updated.FilePath);
    }

    /// <summary>
    /// После удаления номера позиций снова идут подряд: иначе в очереди осталась
    /// бы дыра, и следующий показ сдвинулся бы относительно ожидаемого.
    /// </summary>
    [Fact]
    public async Task DeletingOrderClosesTheGapInNumbering()
    {
        var type = await CreateTypeAsync("коты");
        var first = await CreateOrderAsync(type.Id, "первый.mp4");
        var second = await CreateOrderAsync(type.Id, "второй.mp4");
        var third = await CreateOrderAsync(type.Id, "третий.mp4");

        Assert.True(
            await _service.DeleteMemeOrderAsync(second.Id, TestContext.Current.CancellationToken)
        );

        var orders = (
            await _service.GetMemeOrdersByTypeAsync(type.Id, TestContext.Current.CancellationToken)
        ).ToArray();
        Assert.Equal([1, 2], orders.Select(order => order.Order));
        Assert.Contains(orders, order => order.Id == first.Id);
        Assert.Contains(orders, order => order.Id == third.Id);
    }

    [Fact]
    public async Task UnknownOrderIsNotDeleted()
    {
        Assert.False(
            await _service.DeleteMemeOrderAsync(
                Guid.NewGuid(),
                TestContext.Current.CancellationToken
            )
        );
        Assert.False(
            await _service.DeleteMemeOrderAsync(Guid.Empty, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task OrdersAreRenumberedOnDemand()
    {
        var type = await CreateTypeAsync("коты");
        await CreateOrderAsync(type.Id, "первый.mp4");
        await CreateOrderAsync(type.Id, "второй.mp4");
        await CreateOrderAsync(type.Id, "третий.mp4");

        await _service.ReorderMemeOrdersAsync(type.Id, TestContext.Current.CancellationToken);

        var orders = (
            await _service.GetMemeOrdersByTypeAsync(type.Id, TestContext.Current.CancellationToken)
        ).ToArray();
        Assert.Equal([1, 2, 3], orders.Select(order => order.Order));
    }

    [Fact]
    public async Task ReorderOfUnknownTypeDoesNothing()
    {
        var before = await _service.GetMemeOrderCountAsync(
            null,
            TestContext.Current.CancellationToken
        );

        await _service.ReorderMemeOrdersAsync(0, TestContext.Current.CancellationToken);

        Assert.Equal(
            before,
            await _service.GetMemeOrderCountAsync(null, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task RandomMemeIsTakenFromQueue()
    {
        var type = await CreateTypeAsync("коты");
        await CreateOrderAsync(type.Id, "единственный.mp4");

        var meme = await _service.GetRandomMemeAsync(
            type.Id,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(meme);
        Assert.Equal("единственный.mp4", meme!.FilePath);
    }

    [Fact]
    public async Task RandomMemeOfEmptyQueueIsNull()
    {
        Assert.Null(await _service.GetRandomMemeAsync(null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RandomMemeRespectsRequestedType()
    {
        var cats = await CreateTypeAsync("коты");
        var dogs = await CreateTypeAsync("собаки");
        await CreateOrderAsync(cats.Id, "кот.mp4");
        await CreateOrderAsync(dogs.Id, "собака.mp4");

        var meme = await _service.GetRandomMemeAsync(
            dogs.Id,
            TestContext.Current.CancellationToken
        );

        Assert.NotNull(meme);
        Assert.Equal("собака.mp4", meme!.FilePath);
    }

    [Fact]
    public async Task OrderCountIsReportedForTypeAndOverall()
    {
        var type = await CreateTypeAsync("коты");
        await CreateOrderAsync(type.Id, "кот.mp4");

        var total = await _service.GetMemeOrderCountAsync(
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(
            1,
            await _service.GetMemeOrderCountAsync(type.Id, TestContext.Current.CancellationToken)
        );
        Assert.Equal(
            total,
            await _service.GetMemeOrderCountAsync(null, TestContext.Current.CancellationToken)
        );
        Assert.Equal(
            0,
            await _service.GetMemeOrderCountAsync(999, TestContext.Current.CancellationToken)
        );
    }

    private async Task<MemeType> CreateTypeAsync(string name) =>
        await _service.CreateMemeTypeAsync(
            new MemeType { Name = name, FolderPath = $"Alerts/random_meme/{name}" },
            TestContext.Current.CancellationToken
        );

    private async Task<MemeOrder> CreateOrderAsync(int typeId, string filePath) =>
        await _service.CreateMemeOrderAsync(
            new MemeOrder { FilePath = filePath, MemeTypeId = typeId },
            TestContext.Current.CancellationToken
        );
}

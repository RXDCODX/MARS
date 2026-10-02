using Grpc.Core;
using MARS.Shared.Grpc;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Заглушка хранилища настройки раскладки для процессов без этой таблицы.
/// </summary>
public class UnavailableAdhdConfigStoreTests
{
    [Fact]
    public async Task GetAsync_FailsWithFailedPrecondition()
    {
        var store = new UnavailableAdhdConfigStore();

        var error = await Assert.ThrowsAsync<RpcException>(
            () => store.GetAsync(TestContext.Current.CancellationToken)
        );

        Assert.Equal(StatusCode.FailedPrecondition, error.StatusCode);
        Assert.Equal(UnavailableAdhdConfigStore.Reason, error.Status.Detail);
    }

    [Fact]
    public async Task UpdateAsync_FailsWithFailedPrecondition()
    {
        var store = new UnavailableAdhdConfigStore();

        var error = await Assert.ThrowsAsync<RpcException>(
            () => store.UpdateAsync("{}", TestContext.Current.CancellationToken)
        );

        Assert.Equal(StatusCode.FailedPrecondition, error.StatusCode);
    }
}

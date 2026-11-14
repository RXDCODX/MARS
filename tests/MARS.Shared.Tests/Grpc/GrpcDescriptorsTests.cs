using MARS.Shared.Contracts.Tuna;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Дескрипторы сгенерированных gRPC-сервисов доступны: их читают серверные
/// биндеры, инструменты вроде grpc-рефлексии и тестовые клиенты, поэтому дымовой
/// тест держит их в живом состоянии. Геттер — тривиальный, но он входит в
/// знаменатель покрытия, как и весь сгенерированный код.
/// </summary>
public class GrpcDescriptorsTests
{
    [Fact]
    public void TunaServiceExposesDescriptors()
    {
        Assert.NotNull(TunaService.Descriptor);
        Assert.NotNull(TunaReflection.Descriptor);
    }
}

using Grpc.Core;
using MARS.Platform.Infrastructure.Grpc.Authentication;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

namespace MARS.Shared.Tests.Grpc;

public class ServiceApiKeyGrpcServerInterceptorTests
{
    [Fact]
    public void DisabledAuthenticationAllowsCalls()
    {
        var headers = new Metadata();

        Assert.True(
            ServiceApiKeyGrpcServerInterceptor.IsAuthorized(
                headers,
                new ServiceAuthOptions { ApiKey = null }
            )
        );
    }

    [Fact]
    public void ConfiguredKeyMustMatchExactly()
    {
        var settings = new ServiceAuthOptions { ApiKey = "secret" };

        Assert.True(
            ServiceApiKeyGrpcServerInterceptor.IsAuthorized(
                new Metadata { { "x-api-key", "secret" } },
                settings
            )
        );
        Assert.False(
            ServiceApiKeyGrpcServerInterceptor.IsAuthorized(
                new Metadata { { "x-api-key", "other" } },
                settings
            )
        );
        Assert.False(ServiceApiKeyGrpcServerInterceptor.IsAuthorized(new Metadata(), settings));
    }
}

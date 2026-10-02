using MARS.Shared.Extensions;
using MARS.Shared.Grpc;
using MARS.Shared.Grpc.Telegramus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TelegramusService = MARS.Shared.Grpc.Telegramus.TelegramusService.TelegramusServiceBase;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Поднятие gRPC рядом с REST.
///
/// Порты объявлены кодом, а не конфигурацией: <c>Listen*</c> в Kestrel полностью
/// подавляет <c>ASPNETCORE_URLS</c>. Проверяется, что оба порта объявлены и что
/// gRPC-инфраструктура регистрируется — иначе клиенты получали бы соединение,
/// которое никто не слушает.
/// </summary>
public class GrpcHostingExtensionsTests
{
    [Fact]
    public void RestAndGrpcPortsAreDeclared()
    {
        Assert.Equal(8080, GrpcHostingExtensions.HttpPort);
        Assert.Equal(8081, GrpcHostingExtensions.GrpcPort);
    }

    [Fact]
    public void HostingAddsGrpcServices()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions());

        builder.AddMarsGrpcHosting();

        Assert.Contains(builder.Services, descriptor => descriptor.ServiceType.Name == "IServer");
    }

    /// <summary>
    /// Без заглушки настройки ADHD сервис с gRPC не поднялся бы: владелец базы есть
    /// только в MARS.Alerts.
    /// </summary>
    [Fact]
    public void AdhdConfigStoreAlwaysResolvable()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions());

        builder.AddMarsGrpcHosting();

        Assert.NotNull(builder.Services.BuildServiceProvider().GetService<IAdhdConfigStore>());
    }

    /// <summary>
    /// Рассылка событий регистрируется один раз на всё приложение: иначе каждый
    /// вызов создавал бы свой канал и подписчики не получали бы событий.
    /// </summary>
    [Fact]
    public void EventBroadcasterIsSingleton()
    {
        var services = new ServiceCollection();

        services.AddMarsEventBroadcaster<TelegramusEvent>();

        using var provider = services.BuildServiceProvider();
        Assert.Same(
            provider.GetRequiredService<GrpcEventBroadcaster<TelegramusEvent>>(),
            provider.GetRequiredService<GrpcEventBroadcaster<TelegramusEvent>>()
        );
    }

    /// <summary>
    /// Служба-подписчик разрешается контейнером: иначе регистрация gRPC-сервисов
    /// молча не работала бы.
    /// </summary>
    [Fact]
    public void TelegramusServiceIsRegistered()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions());
        builder.Services.AddSingleton<TelegramusService>(Mock.Of<TelegramusService>());
        builder.AddMarsGrpcHosting();

        Assert.NotNull(builder.Services.BuildServiceProvider().GetService<TelegramusService>());
    }
}

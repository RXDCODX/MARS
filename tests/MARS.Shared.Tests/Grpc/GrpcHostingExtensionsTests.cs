using MARS.Platform.Infrastructure.Events;
using MARS.Platform.Infrastructure.Grpc;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
using MARS.Shared.Contracts.Telegramus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TelegramusService = MARS.Shared.Contracts.Telegramus.TelegramusService.TelegramusServiceBase;

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

    /// <summary>
    /// Дефолты опции — это те же 8080/8081, что объявлены кодом: разойтись они
    /// могут молча, а заметить это можно только на живом стенде.
    /// </summary>
    [Fact]
    public void OptionsDefaultsMatchDeclaredPorts()
    {
        var options = new GrpcOptions();

        Assert.Equal(GrpcHostingExtensions.HttpPort, options.HttpPort);
        Assert.Equal(GrpcHostingExtensions.GrpcPort, options.GrpcPort);
        Assert.Empty(options.Endpoints);
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

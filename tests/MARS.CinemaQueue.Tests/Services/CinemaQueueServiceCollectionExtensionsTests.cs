using MARS.CinemaQueue.Interfaces;
using MARS.CinemaQueue.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MARS.CinemaQueue.Tests.Services;

/// <summary>
/// Регистрация сервисов очереди кино.
///
/// Сервис без зарегистрированного репозитория не поднялся бы: очередь кино
/// молча осталась бы пустой. Проверяются сами регистрации: разрешать их в тесте
/// нельзя, часть зависимостей (БД, опции) добавляется в самом сервисе.
/// </summary>
public class CinemaQueueServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(typeof(ICinemaQueueRepository))]
    [InlineData(typeof(ICinemaQueueService))]
    [InlineData(typeof(IKinopoiskService))]
    [InlineData(typeof(IMediaMetadataService))]
    [InlineData(typeof(ITwitchCinemaQueueService))]
    public void ContractIsRegistered(Type contract)
    {
        var services = new ServiceCollection().AddCinemaQueueServices();

        Assert.Contains(services, descriptor => descriptor.ServiceType == contract);
    }

    /// <summary>
    /// Напоминание о недосмотренных фильмах поднимается вместе с сервисом: без него
    /// зритель узнал бы о забытом фильме только вручную.
    /// </summary>
    [Fact]
    public void NotificationServiceIsHosted()
    {
        var services = new ServiceCollection().AddCinemaQueueServices();

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(CinemaQueueNotificationService)
        );
    }
}

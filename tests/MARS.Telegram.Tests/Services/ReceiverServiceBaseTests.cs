using MARS.Telegram.Data;
using MARS.Telegram.Entities;
using MARS.Telegram.Services.BotService.Abstract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Приём апдейтов Telegram.
///
/// Смещение в базе нужно, чтобы после перезапуска апдейты не приходили дважды, а при
/// пустой базе — наоборот, чтобы старые не проигрывались заново. Проверяется, что
/// получатель берёт смещение из базы и передаёт его в опрос.
/// </summary>
public class ReceiverServiceBaseTests
{
    private readonly ChatTestDbContextFactory _factory = new();

    /// <summary>
    /// Без сохранённого смещения апдейты запрашиваются с начала: иначе после
    /// перезапуска бот молчал бы, пока Telegram не пришлёт что-то новое.
    /// </summary>
    [Fact]
    public async Task ReceiveStartsFromBeginningWithoutOffset()
    {
        var recorder = new UpdateRequestRecorder();
        var client = CreateClient(recorder);
        var stopping = recorder.Stopping;

        await Create(client.Object)
            .ReceiveAsync(
                await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken),
                stopping.Token
            );

        // null означает «пока не знаем, с чего начать»: Telegram сам пришлёт все
        // накопившиеся апдейты.
        Assert.Single(recorder.Offsets);
        Assert.All(recorder.Offsets, offset => Assert.True(offset is null or <= 0));
    }

    /// <summary>
    /// Сохранённое смещение подставляется в запрос: повторная обработка одного
    /// апдейта дважды отправила бы команду стримеру.
    /// </summary>
    [Fact]
    public async Task ReceiveContinuesFromStoredOffset()
    {
        var recorder = new UpdateRequestRecorder();
        var client = CreateClient(recorder);
        var stopping = recorder.Stopping;

        await using (
            var context = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken)
        )
        {
            context.TelegramUpdateReceiverOffsets.Add(
                new TelegramUpdateReceiverOffset { Offset = 4242 }
            );
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await Create(client.Object)
            .ReceiveAsync(
                await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken),
                stopping.Token
            );

        Assert.Equal(4242, Assert.Single(recorder.Offsets));
    }

    /// <summary>
    /// Заглушка перехватывает запросы, а не <c>GetMe</c> и <c>GetUpdates</c>: в
    /// Telegram.Bot 22 это расширяющие методы над <c>SendRequest</c>. Опрос
    /// останавливается отменой токена после первого же запроса.
    /// </summary>
    private static Mock<ITelegramBotClient> CreateClient(UpdateRequestRecorder recorder)
    {
        var client = new Mock<ITelegramBotClient>();
        client
            .Setup(instance =>
                instance.SendRequest(It.IsAny<GetMeRequest>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(
                new User
                {
                    Id = 1,
                    Username = "mars_bot",
                    IsBot = true,
                }
            );
        client
            .Setup(instance =>
                instance.SendRequest(It.IsAny<GetUpdatesRequest>(), It.IsAny<CancellationToken>())
            )
            .Callback<IRequest<Update[]>, CancellationToken>(
                (request, _) => recorder.Record((GetUpdatesRequest)request)
            )
            .ReturnsAsync([]);
        return client;
    }

    /// <summary>
    /// Запросы к Telegram пишутся в список: <c>out</c>-параметр нельзя захватывать
    /// в лямбде, а проверять нужно именно смещение из запроса.
    /// </summary>
    private sealed class UpdateRequestRecorder
    {
        public List<int?> Offsets { get; } = [];

        public CancellationTokenSource Stopping { get; } = new();

        public void Record(GetUpdatesRequest request)
        {
            Offsets.Add(request.Offset);
            Stopping.Cancel();
        }
    }

    private TestReceiverService Create(ITelegramBotClient client) =>
        new(
            client,
            Mock.Of<IUpdateHandler>(),
            NullLogger<ReceiverServiceBase<IUpdateHandler>>.Instance
        );

    private sealed class TestReceiverService(
        ITelegramBotClient client,
        IUpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<IUpdateHandler>> logger
    ) : ReceiverServiceBase<IUpdateHandler>(client, updateHandler, logger);
}

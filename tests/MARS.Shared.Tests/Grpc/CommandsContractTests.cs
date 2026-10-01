using Google.Protobuf;
using MARS.Shared.Grpc.Commands;

namespace MARS.Shared.Tests.Grpc;

/// <summary>
/// Контракт команд ещё не реализуется сервисом, поэтому проверяется не поведение,
/// а форма сообщений: имена и номера полей — это контракт между MARS.Commands и
/// платформами, и их переименование ломает обе стороны молча.
/// </summary>
public class CommandsContractTests
{
    [Fact]
    public void InvokeRequestCarriesParsedArgumentsAndCaller()
    {
        var fields = DescribeFields<InvokeRequest>();

        Assert.Contains("name", fields.Keys);
        Assert.Contains("args", fields.Keys);
        Assert.Contains("platform", fields.Keys);
        Assert.Contains("platform_user_id", fields.Keys);
        Assert.Contains("is_admin", fields.Keys);
        Assert.Contains("correlation_id", fields.Keys);
    }

    /// <summary>
    /// <c>args</c> повторяющееся, а не строка: разбор кавычек делает сам сервис,
    /// иначе каждая платформа разбирала бы ввод по-своему.
    /// </summary>
    [Fact]
    public void ArgsIsRepeated()
    {
        Assert.True(DescribeFields<InvokeRequest>()["args"].IsRepeated);
    }

    [Fact]
    public void ResponseCarriesAttachmentsForMediaCommands()
    {
        var fields = DescribeFields<InvokeResponse>();

        Assert.Contains("attachments", fields.Keys);
        Assert.Contains("job_id", fields.Keys);
    }

    /// <summary>
    /// Отдельного поля с кодом ошибки быть не должно: ошибки передаются
    /// стандартными grpc Status, как уже делают TunaGrpcService
    /// и SoundRequestGrpcService. Своя вторая система разошлась бы с ними.
    /// </summary>
    [Fact]
    public void ResponseHasNoCustomErrorCode()
    {
        var fields = DescribeFields<InvokeResponse>();

        Assert.DoesNotContain("error_code", fields.Keys);
        Assert.DoesNotContain("error", fields.Keys);
    }

    [Fact]
    public void ServiceDeclaresInvokeAndList()
    {
        var service = InvokeRequest.Descriptor.File.Services.Single(s => s.Name == "Commands");

        var methods = service.Methods.Select(m => m.Name).ToArray();

        Assert.Contains("Invoke", methods);
        Assert.Contains("List", methods);
    }

    /// <summary>
    /// Номера полей фиксированы: перестановка ломает wire-совместимость молча,
    /// потому что protobuf переименования не проверяет.
    /// </summary>
    [Fact]
    public void FieldNumbersAreStable()
    {
        var fields = DescribeFields<InvokeRequest>();

        Assert.Equal(1, fields["name"].Number);
        Assert.Equal(2, fields["args"].Number);
        Assert.Equal(3, fields["platform"].Number);
        Assert.Equal(6, fields["is_admin"].Number);
        Assert.Equal(7, fields["correlation_id"].Number);
    }

    [Fact]
    public void PlatformEnumCoversEveryCommandPlatform()
    {
        var values = (CommandPlatform[])
            [
                CommandPlatform.Unspecified,
                CommandPlatform.Telegram,
                CommandPlatform.Twitch,
                CommandPlatform.Discord,
                CommandPlatform.Api,
            ];

        foreach (var expected in values)
        {
            Assert.Contains(expected, Enum.GetValues<CommandPlatform>());
        }
    }

    private static Dictionary<string, (int Number, bool IsRepeated)> DescribeFields<TMessage>()
        where TMessage : IMessage<TMessage>, new()
    {
        return new TMessage()
            .Descriptor.Fields.InFieldNumberOrder()
            .ToDictionary(field => field.Name, field => (field.FieldNumber, field.IsRepeated));
    }
}

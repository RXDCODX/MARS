using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Services.Entitys.Commands;

public class ExampleCommand : BaseCommand
{
    public override string CommandName => "example";
    public override string Description => "Пример команды с несколькими параметрами";
    public override bool IsAdminCommand => false;

    public override CommandParameterInfo[] Parameters =>
        [
            new()
            {
                Name = "name",
                Description = "Имя пользователя",
                Type = CommandParameterType.String,
                Required = true,
            },
            new()
            {
                Name = "age",
                Description = "Возраст",
                Type = CommandParameterType.Int,
                Required = false,
                DefaultValue = "18",
            },
            new()
            {
                Name = "message",
                Description = "Сообщение",
                Type = CommandParameterType.String,
                Required = false,
            },
        ];

    public override Task<CommandResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        Platform platofrm = Platform.None,
        CancellationToken cancellationToken = default
    )
    {
        var name = parameters["name"].ToString() ?? "Неизвестно";
        var age = Convert.ToInt32(parameters["age"]);
        var message = parameters.TryGetValue("message", out var msgObj)
            ? msgObj.ToString()
            : "Привет!";

        var result = $"""
            Привет, {name}!
            Твой возраст: {age}
            Сообщение: {message}

            Пример использования: /example Иван 25 Привет всем!
            """;

        return Task.FromResult(CommandResult.Ok(result));
    }
}

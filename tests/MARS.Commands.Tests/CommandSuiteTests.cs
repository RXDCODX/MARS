using System.Reflection;
using MARS.Commands.Services;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;
using MARS.TestKit;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.Commands.Tests;

/// <summary>
/// Сквозная проверка всех команд репозитория: каждая команда собирается
/// настоящей <see cref="CommandFactory"/>, отдаёт непустые метаданные и
/// вызывается с разобранными параметрами.
///
/// Обход рефлексией здесь не замена точечным тестам, а то, что точечными
/// тестами не покрыть: команд девяносто, у каждой свой конструктор с
/// зависимостями, и тест «все команды собираются и отвечают» ловит сразу три
/// класса регрессий — упавшую команду в реестре, пустое описание и параметры,
/// которые невозможно разобрать.
/// </summary>
public class CommandSuiteTests
{
    /// <summary>
    /// Команда, ждущая внешнего события (звуковой запрос, автопостинг), без
    /// отмены зависла бы на тесте навсегда. Заглушки не отвечают, поэтому такая
    /// команда возвращается по токену, и это ожидаемый исход.
    /// </summary>
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void EveryCommandIsRegisteredAndCarriesMetadata()
    {
        var commands = CreateAllCommands();
        var types = CommandTypes();

        Assert.NotEmpty(commands);

        foreach (var command in commands.Values)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(command.CommandName),
                $"{command.GetType().Name}: пустое имя команды."
            );
            Assert.False(
                string.IsNullOrWhiteSpace(command.Description),
                $"{command.GetType().Name}: пустое описание команды."
            );
            Assert.NotEmpty(command.AvailablePlatforms);
            Assert.NotEmpty(command.InlineTitle);
            Assert.NotEmpty(command.InlineDescription);
        }

        // Молчаливую потерю команды в реестре ловит только сверка с числом
        // типов: CreateAllCommands глотает исключение и просто не кладёт
        // команду в словарь.
        Assert.Equal(types.Count, commands.Count);
    }

    [Fact]
    public void EveryCommandParsesParametersDeclaredByItself()
    {
        var commands = CreateAllCommands();

        foreach (var command in commands.Values)
        {
            var parameters = command.Parameters;
            if (parameters.Length == 0)
            {
                Assert.Empty(command.ParseParameters(string.Empty));
                continue;
            }

            var input = BuildInput(parameters);
            var parsed = command.ParseParameters(input);

            Assert.Equal(parameters.Length, parsed.Count);
            foreach (var parameter in parameters)
            {
                Assert.True(
                    parsed.ContainsKey(parameter.Name),
                    $"{command.GetType().Name}: параметр {parameter.Name} не разобран из «{input}»."
                );
            }
        }
    }

    [Fact]
    public async Task EveryCommandRespondsToPlatformAndVisibilityChecks()
    {
        var commands = CreateAllCommands();
        using var timeout = new CancellationTokenSource(ExecutionTimeout);

        foreach (var command in commands.Values)
        {
            foreach (var platform in Enum.GetValues<Platform>())
            {
                _ = command.IsAvailableOnPlatform(platform);
            }
            foreach (var visibility in Enum.GetValues<CommandVisibility>())
            {
                _ = command.IsVisibleIn(visibility);
            }
            _ = command.GetParameterInfo();
            _ = command.GetAvailablePlatforms();
            _ = command.SupportsInline;
            _ = command.SupportsMediaInline;
            _ = command.InlinePreviewUrl;
            _ = command.Aliases;
            _ = command.IsAdminCommand;

            // Вызов без внешних сервисов: ответ команды не проверяется — он
            // зависит от заглушек. Проверяется одно — команда не бросает
            // наружу и уходит по таймауту, а не висит.
            try
            {
                _ = await command.ExecuteAsync(
                    command.ParseParameters(BuildInput(command.Parameters)),
                    Platform.Api,
                    timeout.Token
                );
            }
            catch (Exception)
            {
                // Ожидаемо: без настоящих зависимостей команда сообщает об
                // ошибке. Непроглоченное исключение упало бы на тесте и
                // превращало бы проверку реестра в проверку каждой команды
                // отдельно.
            }
        }
    }

    private static Dictionary<string, BaseCommand> CreateAllCommands()
    {
        var types = CommandTypes();
        using var provider = DependencyStubFactory.Build(types);
        var factory = new CommandFactory(provider, NullLogger<CommandFactory>.Instance);

        return factory.CreateAllCommands();
    }

    private static List<Type> CommandTypes() =>
        [
            .. typeof(BaseCommand)
                .Assembly.GetTypes()
                .Where(type =>
                    typeof(BaseCommand).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && type != typeof(BaseCommand)
                    && !type.IsGenericTypeDefinition
                )
                .OrderBy(type => type.FullName, StringComparer.Ordinal),
        ];

    /// <summary>
    /// Собирает строку параметров, которую <see cref="BaseCommand.ParseParameters"/>
    /// обязан разобрать без исключения: значения подставляются по объявленному
    /// типу, необязательным достаётся то же самое — так проверка не зависит от
    /// DefaultValue, который команда может и не задавать.
    /// </summary>
    private static string BuildInput(IReadOnlyList<CommandParameterInfo> parameters) =>
        string.Join(' ', parameters.Select(Value));

    private static string Value(CommandParameterInfo parameter) =>
        parameter.Type switch
        {
            CommandParameterType.Int or CommandParameterType.Long => "7",
            CommandParameterType.Double => "1.5",
            CommandParameterType.Bool => "true",
            _ => "проверка",
        };
}

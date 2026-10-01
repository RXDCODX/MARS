using MARS.Commands.Services;
using MARS.Commands.Services.Entitys;
using MARS.Commands.Services.Entitys.Commands;

namespace MARS.Commands.Tests.Commands;

/// <summary>
/// Тип параметра команды был строкой, и <c>ConvertValue</c> падал в ветку
/// <c>_ =&gt; value</c> для всего, чего не знал. Команда <c>download</c>
/// объявляла параметры типа <c>ulong</c> и <c>Message</c> — они молча
/// приезжали как строки, и опечатка в описании параметра стоила бы тихой
/// подмены типа. Тип стал перечислением, и <c>bool</c> больше не превращает
/// «да» в <c>false</c>.
/// </summary>
public sealed class CommandParameterTypeTests
{
    /// <summary>
    /// Параметры команды <c>example</c> объявлены перечислением, поэтому
    /// «неизвестный тип» в принципе невозможно выразить.
    /// </summary>
    [Fact]
    public void DeclaredParameterTypesAreEnumerated()
    {
        var info = new ExampleCommand().GetParameterInfo();

        Assert.Equal(CommandParameterType.String, info[0].Type);
        Assert.Equal(CommandParameterType.Int, info[1].Type);
        Assert.Equal(CommandParameterType.String, info[2].Type);
    }

    /// <summary>
    /// Число обязано разбираться числом: нечисловой ввод падает в
    /// <c>int.Parse</c>, и это лучше тихой строки.
    /// </summary>
    [Fact]
    public void IntRejectsNonNumericInput()
    {
        var command = new ExampleCommand();

        Assert.Throws<FormatException>(() => command.ParseParameters("Иван много"));
    }

    /// <summary>
    /// Ни одна из 67 команд не объявляет параметр типа <c>Bool</c> или
    /// <c>Double</c> — только <c>String</c> (31), <c>Int</c> (5) и <c>Long</c> (2).
    /// Значит ветка <c>"bool" =&gt; value.Equals("true")</c> в старом
    /// <c>ConvertValue</c> была мёртвым кодом, и «да» превращалось в <c>false</c>
    /// только в теории. Проверяется на пробной команде: как только кто-то объявит
    /// <c>Bool</c>, поведение уже будет верным.
    /// </summary>
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public void BoolParsesRealBooleans(string value, bool expected)
    {
        var command = new BoolTypedCommandProbe();

        var parameters = command.ParseParameters(value);

        Assert.Equal(expected, parameters["enabled"]);
    }

    /// <summary>
    /// Раньше «да» и «1» молча давали <c>false</c>, потому что разбор свёлся к
    /// <c>value.Equals("true")</c>. Теперь это ошибка формата с внятным текстом.
    /// </summary>
    [Theory]
    [InlineData("да")]
    [InlineData("1")]
    [InlineData("yes")]
    public void BoolRejectsGarbageInsteadOfSilentlyFalse(string value)
    {
        var command = new BoolTypedCommandProbe();

        Assert.Throws<FormatException>(() => command.ParseParameters(value));
    }
}

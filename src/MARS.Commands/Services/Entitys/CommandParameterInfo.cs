namespace MARS.Commands.Services.Entitys;

public class CommandParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Тип параметра. Перечисление вместо строки: неизвестный тип больше не
    /// приезжает в <c>ConvertValue</c> как строка, а ловится компилятором.
    /// </summary>
    public CommandParameterType Type { get; set; } = CommandParameterType.String;

    public bool Required { get; set; } = true;
    public string? DefaultValue { get; set; }
}

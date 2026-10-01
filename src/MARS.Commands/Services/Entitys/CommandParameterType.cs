namespace MARS.Commands.Services.Entitys;

/// <summary>
/// Тип параметра команды. Был строкой, и всё, чего не знал
/// <c>BaseCommand.ConvertValue</c>, молча приезжало как строка: команда
/// <c>download</c> объявляла параметры <c>ulong</c> и <c>Message</c>, и опечатка
/// в описании стоила бы тихой подмены типа. Перечисление делает такую опечатку
/// ошибкой компиляции.
/// </summary>
public enum CommandParameterType
{
    String,
    Int,
    Long,
    Double,
    Bool,
}

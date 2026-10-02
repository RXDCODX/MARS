using System.Reflection;

namespace MARS.TwitchCore.Tests;

/// <summary>
/// Модели TwitchLib собираются рефлексией: их сеттеры помечены <c>internal</c>,
/// поэтому из тестового проекта объект initializer не компилируется. Отсюда
/// проверяем не TwitchLib, а то, как сервис переносит эти поля в свои модели.
/// </summary>
internal static class TwitchLibModels
{
    public static T Create<T>(params (string Property, object? Value)[] values)
    {
        var result = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

        foreach (var (property, value) in values)
        {
            var target = typeof(T).GetProperty(
                property,
                BindingFlags.Public | BindingFlags.Instance
            )!;
            target.SetValue(result, value);
        }

        return result;
    }
}

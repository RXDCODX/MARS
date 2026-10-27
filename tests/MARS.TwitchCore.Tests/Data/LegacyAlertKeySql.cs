using System.Reflection;
using System.Text.RegularExpressions;
using MARS.TwitchCore.Data.Migrations;

namespace MARS.TwitchCore.Tests.Data;

/// <summary>
/// Доступ к SQL-бэкфиллу миграции <c>AddRewardAlertKey</c> из тестов.
/// </summary>
/// <remarks>
/// Скрипт — <c>private const</c> в миграции: снаружи он не нужен никому, а тесту
/// нужен ровно он, а не его копия. Копия разошлась бы с миграцией молча, и тест
/// проверял бы сам себя.
/// </remarks>
internal static class LegacyAlertKeySql
{
    private const string FieldName = "LegacyAlertKeyBackfillSql";

    private static readonly Regex KeyPattern = new(
        @"\(\s*\d+\s*,\s*'(?<key>[a-z0-9]+)'",
        RegexOptions.CultureInvariant
    );

    /// <summary>Текст SQL, который выполнит миграция.</summary>
    public static string Text
    {
        get
        {
            var constant = typeof(AddRewardAlertKey).GetField(
                FieldName,
                BindingFlags.NonPublic | BindingFlags.Static
            );

            return (string)(constant?.GetRawConstantValue() ?? string.Empty);
        }
    }

    /// <summary>Ключи алертов, которые скрипт проставляет.</summary>
    public static IReadOnlyList<string> Keys =>
        KeyPattern
            .Matches(Text)
            .Select(match => match.Groups["key"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}

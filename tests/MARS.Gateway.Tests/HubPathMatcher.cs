using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace MARS.Gateway.Tests;

/// <summary>
/// Сопоставление пути маршрута YARP с запрошенным путём.
/// </summary>
/// <remarks>
/// Тесты маршрутов читают <c>appsettings.json</c>, а не рантайм, поэтому
/// сопоставление приходится повторять здесь. Парсер взят тот же, что у YARP:
/// <see cref="TemplateParser"/> — иначе проверка была бы написанием второй,
/// незнакомой с AspNetCore логики и врала бы в обе стороны.
/// </remarks>
internal static class HubPathMatcher
{
    /// <summary>
    /// Возвращает <c>true</c>, если маршрут обслуживает указанный путь.
    /// </summary>
    /// <remarks>
    /// Искусственно ограничен одним сегментом <c>{**catchAll}</c> в конце:
    /// именно этим записан catch-all клиента и маршрут хаба, а полная поддержка
    /// шаблонов здесь не нужна и только замаскировала бы проверку.
    /// </remarks>
    public static bool IsMatch(string routeTemplate, string path)
    {
        var normalized = routeTemplate.Replace(
            "**catchAll",
            "**remainder",
            StringComparison.Ordinal
        );
        var parsed = TemplateParser.Parse(normalized);

        if (parsed is not { Segments.Count: > 0 })
        {
            return false;
        }

        var matcher = new TemplateMatcher(parsed, new RouteValueDictionary());

        return matcher.TryMatch(new PathString(path), new RouteValueDictionary());
    }
}

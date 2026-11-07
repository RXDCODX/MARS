using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MARS.TestKit;

/// <summary>
/// Итог обхода контроллеров: неудачи, пропущенные действия и число вызовов.
/// </summary>
public sealed record ControllerSuiteReport(
    IReadOnlyList<string> Failures,
    IReadOnlyList<string> Skipped,
    int ActionsInvoked
)
{
    /// <summary>
    /// Готовое сообщение для <c>Assert.Empty</c>: и неудачи, и пропуски видны
    /// в выводе теста, а не теряются в молчаливом зелёном.
    /// </summary>
    public string Describe() =>
        string.Join(
            Environment.NewLine,
            [
                $"Вызвано действий: {ActionsInvoked}, пропущено: {Skipped.Count}.",
                .. Failures.Select(failure => "ПРОВАЛ " + failure),
                .. Skipped.Select(skipped => "ПРОПУЩЕНО " + skipped),
            ]
        );
}

using MARS.TestKit;

namespace MARS.Admin.Tests.Controllers;

/// <summary>
/// Все действия контроллеров сервиса вызываются с подставляемыми аргументами и
/// обязаны вернуть результат, а не упасть. Настоящие условия проверяют точечные
/// тесты; этот обход ловит то, что ломается молча: забытый аргумент
/// конструктора, значение, на котором действие падает, опечатку в маршруте.
/// </summary>
public class ControllerActionTests
{
    [Fact]
    public void EveryActionAnswers()
    {
        var report = ControllerSuite.Verify("MARS.Admin");

        Assert.True(report.Failures.Count == 0, report.Describe());
        Assert.True(
            report.ActionsInvoked > 0,
            "Ни одного действия не вызвано: обход проверил бы пустоту."
                + Environment.NewLine
                + report.Describe()
        );
    }
}

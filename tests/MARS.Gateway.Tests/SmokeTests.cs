using System.Reflection;

namespace MARS.Gateway.Tests;

public class SmokeTests
{
    /// <summary>
    /// Проверяет, что сборка сервиса реально попала в выходную папку тестов.
    /// Без ссылки на проект пустой тест-проект собирался бы успешно, но проверять
    /// было бы нечего.
    /// </summary>
    [Fact]
    public void ServiceAssemblyIsLoadable()
    {
        var assembly = Assembly.Load("MARS.Gateway");

        Assert.Equal("MARS.Gateway", assembly.GetName().Name);
    }
}

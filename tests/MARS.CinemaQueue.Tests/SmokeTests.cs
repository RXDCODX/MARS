using System.Reflection;

namespace MARS.CinemaQueue.Tests;

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
        var assembly = Assembly.Load("MARS.CinemaQueue.Host");

        Assert.Equal("MARS.CinemaQueue.Host", assembly.GetName().Name);
    }
}

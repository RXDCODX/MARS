using System.Reflection;

namespace MARS.MediaStorage.Tests;

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
        var assembly = Assembly.Load("MARS.MediaStorage");

        Assert.Equal("MARS.MediaStorage", assembly.GetName().Name);
    }
}
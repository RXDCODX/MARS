using MARS.Admin.Entities;

namespace MARS.Admin.Tests.Entities;

/// <summary>
/// Метка имени сервиса на сущностях админки.
/// </summary>
/// <remarks>
/// Имя сервиса в админке показывается человеку, а не используется в коде:
/// опечатка в нём не роняет ничего, но показывает несуществующий сервис.
/// </remarks>
public class ServiceNameAttributeTests
{
    [Fact]
    public void Хранит_Имя_Сервиса()
    {
        var attribute = new ServiceNameAttribute("MARS.Telegram");

        Assert.Equal("MARS.Telegram", attribute.Name);
    }
}

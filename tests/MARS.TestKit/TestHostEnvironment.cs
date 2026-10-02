using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MARS.TestKit;

/// <summary>
/// Заглушка окружения хоста для сервисов, которые в конструкторе или в действии
/// читают пути (wwwroot, каталог хранилища, корень синхронизации).
///
/// Именно заглушка, а не loose-мок: у мока строковые свойства возвращают null,
/// и Path.Combine(null, ...) падал бы с ArgumentNullException — проверка
/// «сервис собирается» ломалась бы не о сервис, а о заглушку.
/// </summary>
public sealed class TestHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;

    public string ApplicationName { get; set; } = "MARS.TestKit";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

    public string WebRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}

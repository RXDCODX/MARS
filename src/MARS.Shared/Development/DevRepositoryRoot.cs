namespace MARS.Shared.Development;

/// <summary>
/// Поиск корня репозитория вверх от каталога содержимого процесса.
/// </summary>
/// <remarks>
/// Один алгоритм на два шага локальной отладки: автостарт инфраструктуры и
/// объявление цели Prometheus. Разъехались бы они не сообщением, а тишиной —
/// файл цели оказался бы в неизвестном месте, и Prometheus его не увидел бы.
/// </remarks>
internal static class DevRepositoryRoot
{
    /// <summary>
    /// Каталог с файлом compose, в котором лежит <c>docker-compose.yml</c>, либо
    /// <c>null</c>, если такого каталога вверх нет.
    /// </summary>
    /// <param name="contentRootPath">
    /// Каталог содержимого процесса: у F5 и <c>dotnet run</c> это каталог проекта.
    /// </param>
    public static string? Locate(string? contentRootPath)
    {
        string? root = null;
        var directory = string.IsNullOrWhiteSpace(contentRootPath)
            ? null
            : new DirectoryInfo(contentRootPath);

        while (directory is not null && root is null)
        {
            if (File.Exists(Path.Combine(directory.FullName, DevInfrastructureCompose.ComposeFile)))
            {
                root = directory.FullName;
            }

            directory = directory.Parent;
        }

        return root;
    }
}

using MARS.Shared.Telegram;

namespace MARS.MediaStorage.Services.Telegram;

/// <summary>
/// Мессенджер, который ничего не отправляет.
/// </summary>
/// <remarks>
/// Нужен, когда токен Telegram не задан. Раньше в этом случае
/// <c>ITelegramAdminMessenger</c> просто не регистрировался, а параметр воркера
/// был объявлен nullable — но nullable-аннотация для контейнера DI не значит
/// ничего: <c>AddHostedService&lt;T&gt;</c> требует, чтобы зависимость была
/// зарегистрирована, и иначе хост падает на старте. На стенде это выглядело как
/// «медиа-хранилище не запускается», хотя токен Telegram к перекодированию
/// мемов отношения не имеет.
/// <para>
/// Заглушка предпочтительнее ручной сборки воркера через
/// <c>sp.GetService&lt;T&gt;()</c>: пустой объект виден в графе зависимостей и
/// позволяет контейнеру проверять, что зависимость разрешается, а не молча
/// оставляет <c>null</c> в поле.
/// </para>
/// </remarks>
public sealed class NullTelegramAdminMessenger : ITelegramAdminMessenger
{
    /// <inheritdoc />
    /// <remarks>
    /// Задача завершается, а не падает: отчёт — это удобство, и отсутствие
    /// Telegram не должно превращать его в повод остановить перекодирование.
    /// </remarks>
    public Task SendAsync(long chatId, string text, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

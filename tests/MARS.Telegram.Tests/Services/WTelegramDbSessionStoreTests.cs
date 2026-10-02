using System.Text;
using MARS.Telegram.Entities;
using MARS.Telegram.Services.WTelegram;
using Microsoft.EntityFrameworkCore;

namespace MARS.Telegram.Tests.Services;

/// <summary>
/// Хранение сессии WTelegram в базе вместо файла.
///
/// Сессия авторизации Telegram — это файл, который библиотека читает и пишет
/// как обычный <see cref="Stream"/>. Здесь она хранится в БД, чтобы переживать
/// перезапуск контейнера. Проверяется именно контракт потока: прочитать сохранённое,
/// записать новое, и то, что после первой записи старый файл больше не нужен.
/// </summary>
public class WTelegramDbSessionStoreTests
{
    private readonly ChatTestDbContextFactory _factory = new();

    /// <summary>
    /// Пустая ба��а даёт поток нулевой длины: клиент WTelegram при первом
    /// запуске авторизуется с нуля, и падать на отсутствии сессии нельзя.
    /// </summary>
    [Fact]
    public void EmptyDatabaseGivesEmptyStream()
    {
        using var store = new WTelegramDbSessionStore(_factory, "Default");

        Assert.Equal(0, store.Length);
        Assert.True(store.CanRead);
        Assert.True(store.CanWrite);
        Assert.False(store.CanSeek);
    }

    [Fact]
    public void SessionIsReadBackFromDatabase()
    {
        SeedSession("Default", "сессия");

        using var store = new WTelegramDbSessionStore(_factory, "Default");

        var buffer = new byte[64];
        var read = store.Read(buffer, 0, buffer.Length);

        Assert.Equal(Encoding.UTF8.GetByteCount("сессия"), store.Length);
        Assert.Equal("сессия", Encoding.UTF8.GetString(buffer, 0, read));
    }

    [Fact]
    public void WrittenSessionIsStoredInDatabase()
    {
        using var store = new WTelegramDbSessionStore(_factory, "Default");

        var payload = Encoding.UTF8.GetBytes("новая сессия");
        store.Write(payload, 0, payload.Length);

        Assert.Equal("новая сессия", Session("Default"));
    }

    /// <summary>
    /// Запись со смещением сохраняет только указанный кусок: WTelegram пишет
    /// блоками, и сохранение всего буфера записало бы мусор после конца данных.
    /// Байты однобайтовые намеренно — иначе длина символов и длина данных в
    /// потоке разошлись бы, и тест проверял бы кодировку, а не смещение.
    /// блоками, и сохранение всего буфера записало бы мусор после конца данных.
    /// </summary>
    [Fact]
    public void WriteWithOffsetStoresRequestedRangeOnly()
    {
        using var store = new WTelegramDbSessionStore(_factory, "Default");
        var buffer = Encoding.ASCII.GetBytes("XXXXdata");

        store.Write(buffer, 4, 4);

        Assert.Equal("data", Session("Default"));
    }

    [Fact]
    public void SecondWriteReplacesSession()
    {
        using var store = new WTelegramDbSessionStore(_factory, "Default");
        store.Write(Encoding.ASCII.GetBytes("first"), 0, 5);

        store.Write(Encoding.ASCII.GetBytes("second"), 0, 6);

        Assert.Equal("second", Session("Default"));
        Assert.Equal(1, SessionCount());
    }

    /// <summary>
    /// Запись, совпадающая по длине с буфером, сохраняется целиком: WTelegram
    /// часто передаёт ровно свой массив.
    /// </summary>
    [Fact]
    public void WriteOfWholeBufferIsStoredAsIs()
    {
        using var store = new WTelegramDbSessionStore(_factory, "Default");
        var buffer = Encoding.UTF8.GetBytes("данные");

        store.Write(buffer, 0, buffer.Length);

        Assert.Equal("данные", Session("Default"));
    }

    /// <summary>
    /// Сессии разных имён не мешают друг другу: в одном процессе может быть
    /// несколько аккаунтов.
    /// </summary>
    [Fact]
    public void SessionsAreIsolatedByName()
    {
        SeedSession("First", "первая");
        SeedSession("Second", "вторая");

        using var first = new WTelegramDbSessionStore(_factory, "First");
        using var second = new WTelegramDbSessionStore(_factory, "Second");

        Assert.Equal(Encoding.UTF8.GetByteCount("первая"), first.Length);
        Assert.Equal(Encoding.UTF8.GetByteCount("вторая"), second.Length);
    }

    /// <summary>
    /// Незасеянная сессия читается как нулевая: клиент не должен падать, если
    /// строки в базе ещё нет.
    /// </summary>
    [Fact]
    public void UnknownSessionIsEmpty()
    {
        using var store = new WTelegramDbSessionStore(_factory, "нет-такой");

        Assert.Equal(0, store.Length);
    }

    /// <summary>
    /// Позиция и длина не поддерживаются: WTelegram работает с сессией как с
    /// непрерывным потоком, и реализация Seek должна быть безопасной заглушкой.
    /// </summary>
    [Fact]
    public void SeekAndSetLengthAreSafeNoops()
    {
        using var store = new WTelegramDbSessionStore(_factory, "Default");

        Assert.Equal(0, store.Seek(0, SeekOrigin.Begin));
        Assert.Equal(0, store.Position);
        store.SetLength(100);
        store.Flush();
        store.Position = 5;
        Assert.Equal(0, store.Position);
    }

    private void SeedSession(string name, string data)
    {
        using var context = _factory.CreateDbContext();
        context.WTelegramSessions.Add(
            new WTelegramSession { Name = name, Data = Encoding.UTF8.GetBytes(data) }
        );
        context.SaveChanges();
    }

    private string Session(string name)
    {
        using var context = _factory.CreateDbContext();

        return Encoding.UTF8.GetString(context.WTelegramSessions.Find(name)!.Data);
    }

    private int SessionCount()
    {
        using var context = _factory.CreateDbContext();

        return context.WTelegramSessions.Count();
    }
}

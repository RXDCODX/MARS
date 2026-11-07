using MARS.MediaStorage.Services.Storage;
using MARS.MediaStorage.Services.Telegram;
using MARS.Shared.Contracts.Telegram;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MARS.MediaStorage.Tests.Services;

/// <summary>
/// Настройки хранилища.
///
/// Лимиты проверяются на границах: файл больше лимита GitHub нельзя запушить,
/// поэтому принимать его в хранилище бессмысленно.
/// </summary>
public class MediaStorageOptionsTests
{
    [Fact]
    public void RetentionDefaultsToThirtyDays()
    {
        var options = new MediaStorageOptions();

        Assert.Equal(30, options.TrashRetentionDays);
        Assert.Equal(TimeSpan.FromDays(30), options.TrashRetention);
    }

    /// <summary>
    /// Лимит загрузки по умолчанию чуть ниже лимита GitHub в 100 МБ.
    /// </summary>
    [Fact]
    public void UploadLimitStaysBelowGitHubLimit()
    {
        var options = new MediaStorageOptions();

        Assert.Equal(95, options.MaxUploadSizeMb);
        Assert.Equal(95L * 1024 * 1024, options.MaxUploadBytes);
    }

    [Fact]
    public void SettingsAreOverridable()
    {
        var options = new MediaStorageOptions
        {
            TrashRetentionDays = 7,
            EnablePurgeWorker = false,
            PurgeIntervalMinutes = 15,
            MaxUploadSizeMb = 10,
        };

        Assert.Equal(TimeSpan.FromDays(7), options.TrashRetention);
        Assert.False(options.EnablePurgeWorker);
        Assert.Equal(15, options.PurgeIntervalMinutes);
        Assert.Equal(10L * 1024 * 1024, options.MaxUploadBytes);
    }

    [Fact]
    public void SectionNameMatchesConfiguration()
    {
        Assert.Equal("MediaStorage", MediaStorageOptions.SectionName);
    }
}

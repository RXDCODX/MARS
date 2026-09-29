using MARS.TwitchCore.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Interfaces;

namespace MARS.TwitchCore.Services.StreamManagement;

/// <summary>
/// Сервис для управления трансляцией Twitch (название, категория, теги)
/// </summary>
public class TwitchStreamManagementService(
    ITwitchAPI api,
    TokenService tokenService,
    ILogger<TwitchStreamManagementService> logger
) : BackgroundService
{
    public bool IsServiceActive { get; set; } = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>
    /// Смена названия трансляции
    /// </summary>
    public async Task<bool> ChangeStreamTitleAsync(string newTitle)
    {
        if (!IsServiceActive)
        {
            logger.LogWarning("Сервис управления трансляцией отключен");
            return false;
        }

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            logger.LogWarning("Название трансляции не может быть пустым");
            return false;
        }

        if (tokenService.Token?.AccessToken == null)
        {
            logger.LogError("Токен доступа недоступен");
            return false;
        }

        try
        {
            var request = new ModifyChannelInformationRequest { Title = newTitle.Trim() };

            await api.Helix.Channels.ModifyChannelInformationAsync(
                TwitchConstants.ChannelId,
                request,
                tokenService.Token.AccessToken
            );

            logger.LogInformation("Название трансляции успешно изменено на: {NewTitle}", newTitle);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return false;
        }
    }

    /// <summary>
    /// Получение текущей информации о трансляции
    /// </summary>
    public async Task<ChannelInformation?> GetStreamInfoAsync()
    {
        if (!IsServiceActive)
        {
            return null;
        }

        if (tokenService.Token?.AccessToken == null)
        {
            logger.LogError("Токен доступа недоступен");
            return null;
        }

        try
        {
            var response = await api.Helix.Channels.GetChannelInformationAsync(
                TwitchConstants.ChannelId,
                tokenService.Token.AccessToken
            );

            return response.Data.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return null;
        }
    }

    /// <summary>
    /// Получение текущего названия трансляции
    /// </summary>
    public async Task<string?> GetCurrentTitleAsync()
    {
        var streamInfo = await GetStreamInfoAsync();
        return streamInfo?.Title;
    }

    /// <summary>
    /// Проверка доступности сервиса
    /// </summary>
    public bool IsServiceAvailable()
    {
        return IsServiceActive && tokenService.Token?.AccessToken != null;
    }
}

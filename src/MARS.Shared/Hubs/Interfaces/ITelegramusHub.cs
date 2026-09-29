using MARS.Shared.Hubs.Models;
using MARS.Shared.Models.Media;

namespace MARS.Shared.Hubs.Interfaces;

public interface ITelegramusHub
{
    Task Alert(MediaDto info);
    Task Alerts(MediaDto[] info);
    Task WaifuRoll(WaifuAlert content, string displayName, string? color = null);
    Task AddNewWaifu(WaifuAlert content, string displayName, string? color = null);
    Task ShowCurrentWife(
        WaifuAlert content,
        HusbandAlert husband,
        string? avatar = null,
        string? color = null
    );
    Task MergeWaifu(
        WaifuAlert content,
        HusbandAlert husband,
        string? avatar = null,
        string? color = null
    );
    Task UpdateWaifuPrizes(object prizes);
    Task FumoFriday(string displayName, string? color = null);
    Task NewMessage(string id, object message);
    Task DeleteMessage(string id);
    Task Highlite(object message, string color, object faceUrl);
    Task PostTwitchInfo(string clientId, string secret);
    Task MakeScreenParticles(object particles);
    Task MakeScreenEmojisParticles(object message);
    Task RandomMem(MediaDto mediaInfo);
    Task AutoMessage(string message);
    Task Adhd(int seconds);
    Task Explosion();
    Task LeroyAlert();
    Task GaoAlert(object gaoAlert);
    Task Credits();
    Task MichaelJackson();
    Task MikuMonday(object mikuMondayData);
    Task MikuMikuBeam(List<object> users);
    Task PhonkEdit();
    Task TikTokEdit(Guid guid, string text);
    Task AllRefund(object user);
    Task AudioQuizStart(object round);
    Task AudioQuizStop();
    Task FumoRoll(
        object fumo,
        object twitchUser,
        string? color = null,
        int collectedCount = 0,
        int totalCount = 0
    );
    Task UpdateFumoPrizes(object prizes);
    Task FrogRoll(object frog, object twitchUser, string? color = null);
    Task UpdateFrogPrizes(object prizes);
    Task MikuRoll(
        object mikuModule,
        object twitchUser,
        string? color = null,
        int collectedCount = 0,
        int totalCount = 0
    );
    Task UpdateMikuPrizes(object prizes);
}

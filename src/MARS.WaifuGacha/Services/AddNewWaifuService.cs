using MARS.Shared.Clients;
using MARS.Shared.Models;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.WaifuGacha.Services;

public class AddNewWaifuService(
    ILogger<AddNewWaifuService> logger,
    IShikimoriApiClient shikimoriClient,
    WaifuRollService waifuRollService,
    WaifuRollEnsurenceService waifuDbHelper,
    WaifuRollGuaranteeService guaranteeService
)
{
    private const int GuaranteeRolls = 200;

    public async Task<OperationResult<AddNewWaifuResult>> AddNewWaifuAsync(
        string userInput,
        string userId,
        string displayName,
        bool isVip
    )
    {
        var id = GetShikimoriCharacterIdFromLink(userInput);

        if (id == 0)
        {
            return OperationResult<AddNewWaifuResult>.Fail(
                "Не удалось добавить супруга, кривая ссылка! :-("
            );
        }

        var character = await shikimoriClient.GetCharacterAsync(id);

        if (character is null)
        {
            return OperationResult<AddNewWaifuResult>.Fail(
                "Не удалось добавить супруга, проблема с ссылкой и получением с неё id персонажа! :-("
            );
        }

        var waifuResult = await waifuRollService.AddNewWaifu(character);

        if (!waifuResult.Success || waifuResult.Result?.Waifu is null)
        {
            return OperationResult<AddNewWaifuResult>.Fail(
                waifuResult.ErrorMessage ?? "Не удалось добавить супруга! :-("
            );
        }

        var waifu = waifuResult.Result.Waifu;
        waifu.IsAdded = true;

        waifu = await waifuDbHelper.EnsureMangaAndAnimeTitleExists(waifu);

        var result = new AddNewWaifuResult { Waifu = waifu, DisplayName = displayName };

        if (!isVip)
        {
            await guaranteeService.IncrementRollCountAsync(userId);
            var vipDropped = await guaranteeService.CheckVipDropAsync(userId);

            if (vipDropped.Result?.IsVipDropped == true)
            {
                result.VipDropped = true;
            }
            else
            {
                var guaranteeInfo = await guaranteeService.GetGuaranteeInfoAsync(userId);
                result.RollsUntilGuarantee =
                    GuaranteeRolls - (guaranteeInfo.Result?.RollCount ?? 0);
            }
        }

        return OperationResult<AddNewWaifuResult>.Ok(result);
    }

    /// <summary>
    /// Id персонажа из ссылки вида <c>https://shikimori.one/characters/1-naruto</c>.
    /// Хост больше не сверяется с настройкой сервиса: зритель может прислать
    /// зеркало сайта, а персонаж у Shikimori один.
    /// </summary>
    private long GetShikimoriCharacterIdFromLink(string url)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            url,
            @"characters/([a-zA-Z]*\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (!match.Success)
        {
            return 0;
        }

        var characterId = match.Groups[1].Value;
        return long.Parse(characterId);
    }
}

public class AddNewWaifuResult
{
    public Waifu? Waifu { get; set; }
    public string? DisplayName { get; set; }
    public bool VipDropped { get; set; }
    public int RollsUntilGuarantee { get; set; }
}

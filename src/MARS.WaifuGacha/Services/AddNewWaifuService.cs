using MARS.Shared.Models;
using MARS.WaifuGacha.Data;
using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MARS.WaifuGacha.Services;

public class AddNewWaifuService(
    ILogger<AddNewWaifuService> logger,
    ShikimoriService shikimoriService,
    IOptions<ShikimoriClientOptions> options,
    WaifuRollService waifuRollService,
    WaifuRollEnsurenceService waifuDbHelper,
    WaifuRollGuaranteeService guaranteeService
)
{
    private readonly ShikimoriClientOptions _options = options.Value;
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

        var character = await shikimoriService.GetShikiCharacterById(id);

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
        waifu.ImageUrl = _options.ShikimoriSite + waifu.ImageUrl;

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

    private long GetShikimoriCharacterIdFromLink(string url)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            $"{_options.ShikimoriSite}/characters/([a-zA-Z]*\\d+)"
        );

        var match = regex.Match(url);

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

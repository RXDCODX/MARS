using System.Text;
using MARS.TwitchCore.Extensions;
using MARS.TwitchCore.Services.Rewards;
using Microsoft.Extensions.Logging;
using TwitchLib.Client.Interfaces;

namespace MARS.TwitchCore.Entities.Subs;

public class VictorinaGame(
    ILogger logger,
    ITwitchClient client,
    ITwitchTrivia triviaService,
    int timeoutBetweenHints,
    CancellationTokenSource? tokenSource,
    SemaphoreSlim semaphoreSlim,
    HashSet<string> noWaifuHelpUsers
)
{
    private readonly List<VictorinaLetter> _listLetters = [];
    public string Answer = "";

    public bool Active { get; set; } = true;
    public bool AllLettersShowed { get; set; }
    public bool SkipQuestion { get; set; }

    private async Task StartQuestion()
    {
        var numberQuestion = Random.Shared.Next(0, triviaService.CountQuestions);
        var strQuestion = await triviaService.GetQuestionAsync(numberQuestion);
        var arrQuestion = strQuestion.Split('|');
        Answer = arrQuestion[1];

        foreach (var t in Answer)
        {
            _listLetters.Add(new VictorinaLetter { Letter = t, Showed = false });
        }

        await client.SendMessageToMainTwitchAsync(
            $"({Answer.Length} букв): {arrQuestion[0]}",
            logger
        );
    }

    public async Task MainThread()
    {
        try
        {
            AllLettersShowed = false;
            SkipQuestion = false;
            await StartQuestion();

            while (!AllLettersShowed && !SkipQuestion)
            {
                await Task.Delay(timeoutBetweenHints * 1000, tokenSource!.Token);

                if (!Active)
                {
                    return;
                }

                await semaphoreSlim.WaitAsync(tokenSource.Token);
                if (SkipQuestion)
                {
                    SkipQuestion = false;
                    break;
                }

                semaphoreSlim.Release();

                var founded = false;
                while (!founded)
                {
                    var indLetter = Random.Shared.Next(0, Answer.Length);
                    if (!_listLetters[indLetter].Showed)
                    {
                        _listLetters[indLetter].Showed = true;
                        founded = true;
                    }
                }

                await semaphoreSlim.WaitAsync(tokenSource.Token);
                if (SkipQuestion)
                {
                    SkipQuestion = false;
                    break;
                }

                semaphoreSlim.Release();

                var strHint = new StringBuilder("");
                foreach (VictorinaLetter itemLetter in _listLetters)
                {
                    strHint.Append(itemLetter.Showed ? itemLetter.Letter : '_');

                    strHint.Append(' ');
                }

                await semaphoreSlim.WaitAsync(tokenSource.Token);

                if (SkipQuestion)
                {
                    SkipQuestion = false;
                    break;
                }

                if (AllLettersShowed)
                {
                    break;
                }

                var countLetters = _listLetters.Count(e => e.Showed);
                var allLettersAreValid = countLetters == _listLetters.Count;

                if (allLettersAreValid)
                {
                    AllLettersShowed = true;
                    await client.SendMessageToMainTwitchAsync(
                        $"Никто не отгадал! Ответ: {strHint}",
                        logger
                    );
                    noWaifuHelpUsers.Clear();
                }
                else
                {
                    await client.SendMessageToMainTwitchAsync($"Подсказка: {strHint}", logger);
                }

                semaphoreSlim.Release();
            }

            Active = false;
            SkipQuestion = false;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }
        finally
        {
            Active = false;
        }
    }
}

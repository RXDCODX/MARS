using Microsoft.Extensions.Hosting;
using MARS.TwitchCore.Services.Rewards;

namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Локальный источник вопросов викторины.
/// Файл <c>Trivia/bot_trivia_questions.txt</c> копируется в выходную папку проекта
/// и читается целиком один раз: в монолите <c>File.ReadAllLinesAsync</c> вызывался
/// на каждый вопрос (134k строк на вопрос), что съедало CPU и память.
/// Формат строки: <c>вопрос|ответ</c>.
/// </summary>
public class TwitchTriviaService(
    IHostEnvironment environment,
    ILogger<TwitchTriviaService> logger
) : ITwitchTrivia
{
    private const string QuestionSeparator = "|";

    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private volatile string[]? _questions;

    public int CountQuestions => _questions?.Length ?? 0;

    public string FilenameTrivia =>
        Path.Combine(environment.ContentRootPath, "Trivia", "bot_trivia_questions.txt");

    public async Task<string> GetQuestionAsync(int numberQuestion)
    {
        var questions = await EnsureLoadedAsync();
        var result = string.Empty;

        if (questions.Length > 0)
        {
            var index = numberQuestion;

            if (index < 0 || index >= questions.Length)
            {
                index = Random.Shared.Next(questions.Length);
            }

            result = questions[index];
        }

        return result;
    }

    private async Task<string[]> EnsureLoadedAsync()
    {
        var cached = _questions;

        if (cached is not null)
        {
            return cached;
        }

        await _loadSemaphore.WaitAsync();

        try
        {
            cached = _questions;

            if (cached is null)
            {
                var filename = FilenameTrivia;

                if (!File.Exists(filename))
                {
                    logger.LogError("Файл вопросов викторины не найден: {Filename}", filename);
                    cached = [];
                }
                else
                {
                    var lines = await File.ReadAllLinesAsync(filename);

                    cached =
                    [
                        .. lines.Where(line =>
                            !string.IsNullOrWhiteSpace(line) && line.Contains(QuestionSeparator)
                        ),
                    ];

                    logger.LogInformation(
                        "Загружено {Count} вопросов викторины из {Filename}",
                        cached.Length,
                        filename
                    );
                }

                _questions = cached;
            }
        }
        finally
        {
            _loadSemaphore.Release();
        }

        return cached;
    }
}

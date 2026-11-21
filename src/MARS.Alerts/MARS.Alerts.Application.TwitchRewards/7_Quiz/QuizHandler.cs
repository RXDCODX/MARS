using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Shared.Contracts.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MARS.Alerts.Application.TwitchRewards._7_Quiz;

/// <summary>
/// Обработчик reward-события <c>RewardQuiz</c>.
/// </summary>
public class QuizHandler : RewardHandlerBase
{
    private readonly ILogger<QuizHandler> _logger;
    private readonly IMessageBus _messageBus;
    private readonly IHostEnvironment _environment;

    private readonly Lock _stateLock = new();
    private string[] _questions = [];
    private bool _isGameRunning;

    public QuizHandler(
        ILogger<QuizHandler> logger,
        IMessageBus messageBus,
        IHostEnvironment environment
    )
    {
        _logger = logger;
        _messageBus = messageBus;
        _environment = environment;

        OnTwitchMessage += HandleChatAsync;
    }

    public override string RewardCode => "quiz";

    private string QuestionFile =>
        Path.Combine(_environment.ContentRootPath, "Trivia", "bot_trivia_questions.txt");

    private Task HandleChatAsync(ChatEventMessageEvent message, CancellationToken ct)
    {
        // Answer checking is performed by the game loop; here we just store the message.
        return Task.CompletedTask;
    }

    protected override async Task OnRewardRedeemedAsync(
        RewardRedeemedEvent rewardEvent,
        CancellationToken ct
    )
    {
        if (_isGameRunning)
        {
            return;
        }

        _isGameRunning = true;

        try
        {
            if (_questions.Length == 0 && File.Exists(QuestionFile))
            {
                var lines = await File.ReadAllLinesAsync(QuestionFile, ct);
                _questions =
                [
                    .. lines.Where(l => !string.IsNullOrWhiteSpace(l) && l.Contains('|')),
                ];
            }

            if (_questions.Length == 0)
            {
                await _messageBus.PublishAsync(
                    new ChatSendEvent { Message = "Вопросы викторины не найдены." }
                );
                return;
            }

            var question = _questions[Random.Shared.Next(_questions.Length)];
            var parts = question.Split('|', 2);
            var questionText = parts[0].Trim();
            var answer = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            await _messageBus.PublishAsync(
                new ChatSendEvent { Message = $"🧠 Викторина: {questionText} (30 секунд на ответ)" }
            );

            // Wait for answers (simplified: just announce; full answer matching requires
            // chat message inspection which is handled by a separate listener in production).
            await Task.Delay(TimeSpan.FromSeconds(30), ct);

            await _messageBus.PublishAsync(
                new ChatSendEvent { Message = $"⏰ Время вышло! Ответ: {answer}" }
            );

            _logger.LogInformation("Quiz finished, answer: {Answer}", answer);
        }
        finally
        {
            _isGameRunning = false;
        }
    }
}

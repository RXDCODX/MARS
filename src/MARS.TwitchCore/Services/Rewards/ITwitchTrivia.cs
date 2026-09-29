namespace MARS.TwitchCore.Services.Rewards;

/// <summary>
/// Minimal interface for trivia question lookup.
/// Implemented by the trivia game service that owns the question file.
/// </summary>
public interface ITwitchTrivia
{
    Task<string> GetQuestionAsync(int numberQuestion);
    int CountQuestions { get; }
}

using MARS.TwitchCore.Services.Rewards;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace MARS.TwitchCore.Tests.Services;

/// <summary>
/// Источник вопросов викторины: файл читается один раз и дальше берётся из памяти.
///
/// Проверяется именно этот контракт — в монолите файл читался на каждый вопрос
/// (134k строк на вопрос). Отдельно проверяется, что битые строки не портят
/// разбор, иначе вопросом мог бы оказаться пустая строка.
/// </summary>
public class TwitchTriviaServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mars-trivia-tests",
        Guid.NewGuid().ToString("N")
    );
    private readonly TwitchTriviaService _service;

    /// <summary>
    /// Корень — временная папка, а не папка тестов: настоящий файл вопросов
    /// копируется в выходную папку проекта, и пустой content root читал бы его
    /// вместо пустоты.
    /// </summary>
    public TwitchTriviaServiceTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Trivia"));
        _service = new TwitchTriviaService(
            new TempEnvironment(_root),
            NullLogger<TwitchTriviaService>.Instance
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void FileNameIsInsideContentRoot()
    {
        var service = new TwitchTriviaService(
            new TempEnvironment("D:\\content"),
            NullLogger<TwitchTriviaService>.Instance
        );

        Assert.Equal(
            Path.Combine("D:\\content", "Trivia", "bot_trivia_questions.txt"),
            service.FilenameTrivia
        );
    }

    /// <summary>
    /// До первой загрузки счётчик вопросов нулевой: иначе викторина решила бы,
    /// что вопросов нет, и не показала бы их никогда.
    /// </summary>
    [Fact]
    public void CountIsZeroBeforeFirstLoad()
    {
        Assert.Equal(0, _service.CountQuestions);
    }

    [Fact]
    public async Task QuestionByIndexIsReturned()
    {
        Write("первый|ответ", "второй|ответ");

        Assert.Equal("первый|ответ", await _service.GetQuestionAsync(0));
        Assert.Equal(2, _service.CountQuestions);
    }

    /// <summary>
    /// Номер за пределами файла не роняет викторину: вместо пустого вопроса
    /// зритель получает случайный из загруженных.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    public async Task IndexOutsideFileYieldsRandomQuestion(int number)
    {
        Write("первый|ответ", "второй|ответ");

        var question = await _service.GetQuestionAsync(number);

        Assert.Contains(question, new[] { "первый|ответ", "второй|ответ" });
    }

    [Fact]
    public async Task BlankAndBrokenLinesAreSkipped()
    {
        Write("", "   ", "без разделителя", "вопрос|ответ");

        Assert.Equal("вопрос|ответ", await _service.GetQuestionAsync(0));
        Assert.Equal(1, _service.CountQuestions);
    }

    /// <summary>
    /// Отсутствующий файл вопросов даёт пустой вопрос, а не исключение: файл не
    /// едет вместе с образом, и падать из-за него нельзя.
    /// </summary>
    [Fact]
    public async Task MissingFileYieldsEmptyQuestion()
    {
        Assert.Equal(string.Empty, await _service.GetQuestionAsync(0));
        Assert.Equal(0, _service.CountQuestions);
    }

    /// <summary>
    /// Файл читается один раз: правка на диске после первой загрузки не должна
    /// менять вопросы на лету — иначе викторина едет в середине игры.
    /// </summary>
    [Fact]
    public async Task QuestionsAreCachedAfterFirstLoad()
    {
        Write("первый|ответ");

        Assert.Equal("первый|ответ", await _service.GetQuestionAsync(0));

        Write("другой|ответ");

        Assert.Equal("первый|ответ", await _service.GetQuestionAsync(0));
    }

    private void Write(params string[] lines) =>
        File.WriteAllText(
            Path.Combine(_root, "Trivia", "bot_trivia_questions.txt"),
            string.Join("\n", lines)
        );

    private sealed class TempEnvironment(string contentRoot) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "MARS.TwitchCore.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = contentRoot;

        public string EnvironmentName { get; set; } = Environments.Development;
    }
}

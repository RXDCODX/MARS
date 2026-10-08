using MARS.Shared.Matoi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MARS.Shared.Tests.Matoi;

/// <summary>
/// Регистрация клиента matoi в контейрере.
/// </summary>
public class MatoiServiceCollectionExtensionsTests
{
    private const string Section = """
        {
          "Matoi": {
            "BaseUrl": "http://matoi:3000/",
            "ApiKey": "mars-dev-matoi-key",
            "DefaultProvider": "danbooru",
            "TimeoutSeconds": 7
          }
        }
        """;

    /// <summary>
    /// Сокеты переиспользуются, а сам клиент может быть transient.
    /// </summary>
    /// <remarks>
    /// <c>AddHttpClient&lt;TClient, TImplementation&gt;</c> регистрирует типизированный
    /// клиент как transient — и это правильно: он держит <see cref="HttpClient"/>,
    /// а тот не должен жить дольше обработчика. Экономить надо не на экземпляре
    /// клиента, а на соединениях, поэтому проверяется общий обработчик: если он
    /// разный, то каждый вызов награды открывал бы свой сокет.
    /// </remarks>
    [Fact]
    public void СокетыПереиспользуютсяЧерезОбщийОбработчик()
    {
        using var provider = Provider(Section);
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        var first = factory.CreateClient(MatoiServiceCollectionExtensions.HttpClientName);
        var second = factory.CreateClient(MatoiServiceCollectionExtensions.HttpClientName);

        Assert.NotNull(provider.GetRequiredService<IMatoiPostService>());

        // Экземпляры HttpClient разные, обработчик под ними — один.
        Assert.NotSame(first, second);
        Assert.Same(GetHandler(first), GetHandler(second));
    }

    [Fact]
    public void КлиентРешаетсяПриКаждомОбращении()
    {
        using var provider = Provider(Section);

        var first = provider.GetRequiredService<IMatoiPostService>();
        var second = provider.GetRequiredService<IMatoiPostService>();

        Assert.IsType<MatoiPostService>(first);
        Assert.IsType<MatoiPostService>(second);
    }

    [Fact]
    public void НастройкиЧитаютсяИзСекцииMatoi()
    {
        using var provider = Provider(Section);

        var options = provider.GetRequiredService<IOptions<MatoiOptions>>().Value;

        Assert.Equal("http://matoi:3000/", options.BaseUrl);
        Assert.Equal("mars-dev-matoi-key", options.ApiKey);
        Assert.Equal("danbooru", options.DefaultProvider);
        Assert.Equal(7, options.TimeoutSeconds);
    }

    [Fact]
    public void ИмяСекцииСовпадаетСКонфигурациейCompose()
    {
        // В compose переменные названы Matoi__BaseUrl и Matoi__ApiKey, поэтому
        // имя секции обязано быть ровно таким: смена одного слога развела бы
        // адрес и ключ по разным настройкам, и клиент ушёл бы без ключа.
        Assert.Equal("Matoi", MatoiOptions.SectionName);
    }

    [Fact]
    public void ТаймаутБерётсяИзНастроек()
    {
        using var provider = Provider(Section);

        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(MatoiServiceCollectionExtensions.HttpClientName);

        Assert.Equal(TimeSpan.FromSeconds(7), client.Timeout);
        Assert.Equal("http://matoi:3000/", client.BaseAddress?.ToString());
    }

    [Fact]
    public void РегистрацияНеПадаетБезСекции()
    {
        // Секции может не быть: matoi тогда не настроен, и клиент обязан
        // сообщить об этом сам (см. пустой BaseUrl), а не уронить старт сервиса
        // при чтении конфигурации.
        using var provider = Provider("{}");

        var options = provider.GetRequiredService<IOptions<MatoiOptions>>().Value;

        Assert.Equal(string.Empty, options.BaseUrl);
        Assert.NotNull(provider.GetRequiredService<IMatoiPostService>());
    }

    private static ServiceProvider Provider(string json)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();

        return new ServiceCollection().AddMatoiClient(configuration).BuildServiceProvider();
    }

    /// <summary>
    /// Обработчик из клиента <see cref="IHttpClientFactory"/>.
    /// </summary>
    /// <remarks>
    /// Достаётся рефлексией: публичного способа спросить обработчик у
    /// <see cref="HttpClient"/> нет, а проверять надо именно его — общий или
    /// свой у каждого экземпляра.
    /// </remarks>
    private static object? GetHandler(HttpClient client)
    {
        var field = typeof(HttpMessageInvoker).GetField(
            "_handler",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        return field?.GetValue(client);
    }
}

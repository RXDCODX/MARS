using Microsoft.Playwright;

namespace MARS.ClientUi.Tests;

/// <summary>
/// Фикстура на весь набор: один браузер, один стенд.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ClientUiCollection : ICollectionFixture<ClientUiFixture>
{
    public const string Name = "client-ui";
}

using Microsoft.EntityFrameworkCore;
using MARS.Telegram.Data;
using MARS.Telegram.Entities;

namespace MARS.Telegram.Tests;

/// <summary>
/// Проверки схемы таблиц автопостинга в mars_chat.
/// Утверждения берутся из собранной модели EF: HasMaxLength, HasIndex и
/// DeleteBehavior — реляционные аннотации, и они присутствуют в модели
/// независимо от провайдера.
/// </summary>
public class BooruSchemaTests
{
    private static ChatDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase("test")
            .Options;

        return new ChatDbContext(options);
    }

    [Theory]
    [InlineData(typeof(BooruAutoPostConfig), "BooruAutoPostConfigs")]
    [InlineData(typeof(BooruScheduledPost), "BooruScheduledPosts")]
    public void BooruTables_LiveInChatSchemaWithLegacyNames(Type clrType, string tableName)
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(clrType);

        Assert.Equal("chat", entity!.GetSchema());
        Assert.Equal(tableName, entity.GetTableName());
    }

    /// <summary>
    /// В монолите DiscordChannelId был varchar(64). Без явного ограничения
    /// значение усекалось бы уже в базе, и поиск по каналу перестал бы работать.
    /// </summary>
    [Fact]
    public void BooruAutoPostConfig_DiscordChannelIdKeepsLegacyLength()
    {
        using var context = CreateContext();

        var property = context
            .Model.FindEntityType(typeof(BooruAutoPostConfig))!
            .FindProperty(nameof(BooruAutoPostConfig.DiscordChannelId));

        Assert.Equal(64, property!.GetMaxLength());
    }

    /// <summary>
    /// Ссылка каскадная: удаление правила должно уносить его расписание,
    /// иначе в таблице остаются строки с несуществующим ConfigId.
    /// </summary>
    [Fact]
    public void BooruScheduledPost_CascadeDeletesWithConfig()
    {
        using var context = CreateContext();

        var fk = context
            .Model.FindEntityType(typeof(BooruScheduledPost))!
            .GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(BooruAutoPostConfig));

        Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);
    }

    [Fact]
    public void BooruScheduledPost_HasIndexOnConfigId()
    {
        using var context = CreateContext();

        var indexes = context.Model.FindEntityType(typeof(BooruScheduledPost))!.GetIndexes();

        Assert.Contains(indexes, i => i.Properties.Single().Name == nameof(BooruScheduledPost.ConfigId));
    }

    /// <summary>
    /// Числовые значения enum хранятся как int и обязаны совпадать с монолитом:
    /// иначе перенесённые строки получили бы другой смысл (например,
    /// Status=1 стал бы Pending вместо Posted).
    /// </summary>
    [Fact]
    public void BooruEnums_MatchLegacyNumericValues()
    {
        Assert.Equal(0, (int)BooruSource.Danbooru);
        Assert.Equal(1, (int)BooruSource.Rule34);
        Assert.Equal(0, (int)BooruTargetPlatform.Discord);
        Assert.Equal(1, (int)BooruTargetPlatform.Telegram);
        Assert.Equal(0, (int)BooruTelegramParseMode.Default);
        Assert.Equal(1, (int)BooruTelegramParseMode.Html);
        Assert.Equal(2, (int)BooruTelegramParseMode.Markdown);
        Assert.Equal(0, (int)BooruScheduledPostStatus.Pending);
        Assert.Equal(1, (int)BooruScheduledPostStatus.Posted);
        Assert.Equal(2, (int)BooruScheduledPostStatus.Failed);
        Assert.Equal(3, (int)BooruScheduledPostStatus.Cancelled);
    }
}

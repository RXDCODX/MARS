using MARS.MediaStorage.Entities;
using MARS.Shared.Models.Media;

namespace MARS.MediaStorage.Extensions;

public static class MediaInfoExtension
{
    public static MediaInfo FixAlertText(this MediaInfo media, string username, string usertext)
    {
        if (
            media
                .TextInfo.Text?.ToLower()
                .Contains("{user.text}", StringComparison.CurrentCultureIgnoreCase) ?? false
        )
        {
            media.TextInfo.Text = usertext.StartsWith('@')
                ? media.TextInfo.Text.Replace("{user.text}", usertext[1..].Trim())
                : media.TextInfo.Text.Replace("{user.text}", usertext.Trim());
        }

        if (
            media.TextInfo.Text?.Contains("{user.name}", StringComparison.OrdinalIgnoreCase)
            ?? false
        )
        {
            media.TextInfo.Text = media.TextInfo.Text.Replace("{user.name}", username);
        }

        return media;
    }

    public static MediaInfo FixAlertColor(this MediaInfo media, string chatColor)
    {
        if (media.TextInfo.KeyWordsColor?.Contains("{user.color}") ?? false)
        {
            media.TextInfo.KeyWordsColor = media.TextInfo.KeyWordsColor.Replace(
                "{user.color}",
                chatColor
            );
        }

        if (media.TextInfo.TextColor?.Contains("{user.color}") ?? false)
        {
            media.TextInfo.TextColor = media.TextInfo.TextColor?.Replace("{user.color}", chatColor);
        }

        return media;
    }
}

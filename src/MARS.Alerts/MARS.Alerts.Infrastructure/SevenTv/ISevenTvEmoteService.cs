using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SevenTV;

namespace MARS.Alerts.Infrastructure.SevenTv;

public interface ISevenTvEmoteService
{
    bool IsEmote(string word);
}

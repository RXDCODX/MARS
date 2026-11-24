using MARS.Twitch.Application.Models;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Infrastructure.Data;
using MARS.Twitch.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MARS.Twitch.Infrastructure.Services.AutoMessages;

public interface IAutoMessagesService
{
    Task<IEnumerable<AutoMessageDto>> GetAllAutoMessagesAsync(
        CancellationToken cancellationToken = default
    );
    Task<AutoMessageDto?> GetAutoMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
    Task<AutoMessageDto> CreateAutoMessageAsync(
        CreateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    );
    Task<AutoMessageDto?> UpdateAutoMessageAsync(
        Guid id,
        UpdateAutoMessageRequest request,
        CancellationToken cancellationToken = default
    );
    Task<bool> DeleteAutoMessageAsync(Guid id, CancellationToken cancellationToken = default);
}

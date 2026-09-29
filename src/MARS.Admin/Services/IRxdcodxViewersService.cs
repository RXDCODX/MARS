using MARS.Admin.Entities;

namespace MARS.Admin.Services;

/// <summary>
/// Интерфейс для сервиса получения информации о зрителях канала rxdcodx
/// </summary>
public interface IRxdcodxViewersService
{
    Task<List<FollowerInfo>?> GetAllFollowersInfo(bool useCash = false);
    Task<List<FollowerInfo>> GetUsersWithoutAvatarsAsync();
    Task<int> GetUsersWithoutAvatarsCountAsync();
    Task<int> UpdateMissingAvatarsAsync();
}

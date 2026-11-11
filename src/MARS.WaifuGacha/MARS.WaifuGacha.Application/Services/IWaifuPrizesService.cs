using System.Collections.Generic;
using System.Threading.Tasks;
using MARS.Shared.Contracts.Models;
using MARS.WaifuGacha.Domain.Entities;

namespace MARS.WaifuGacha.Application.Services;

public interface IWaifuPrizesService
{
    /// <summary>
    /// Получение призов вайфу
    /// </summary>
    /// <returns>Результат получения призов</returns>
    Task<OperationResult<ICollection<PrizeType>>> GetWaifuPrizesAsync();
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MARS.Shared.Models;
using MARS.WaifuGacha.Entities;

namespace MARS.WaifuGacha.Services.Interfaces;

public interface IWaifuPrizesService
{
    /// <summary>
    /// Получение призов вайфу
    /// </summary>
    /// <returns>Результат получения призов</returns>
    Task<OperationResult<ICollection<PrizeType>>> GetWaifuPrizesAsync();
}

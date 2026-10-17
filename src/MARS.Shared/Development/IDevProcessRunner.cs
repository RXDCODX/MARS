namespace MARS.Shared.Development;

/// <summary>
/// Запуск внешнего процесса и ожидание его завершения.
/// </summary>
/// <remarks>
/// Синхронный намеренно: шаг идёт до старта приложения, а не в рабочем пути.
/// Исключений не бросает — отказ возвращается результатом, потому что
/// отсутствующий <c>docker</c> не должен ронять сервис: он сам упадёт дальше на
/// миграциях и получит там своё сообщение. По образцу <c>IGitCommandExecutor</c>.
/// </remarks>
public interface IDevProcessRunner
{
    /// <summary>Выполняет процесс и ждёт завершения.</summary>
    /// <param name="request">Что запускать и сколько ждать.</param>
    DevProcessResult Run(DevProcessRequest request);
}

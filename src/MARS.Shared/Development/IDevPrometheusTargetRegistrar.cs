namespace MARS.Shared.Development;

/// <summary>
/// Объявляет процесс, запущенный на хосте, целью Prometheus.
/// </summary>
/// <remarks>
/// В списке целей Prometheus стоят имена контейнеров, поэтому сервис на хосте
/// (F5 или <c>scripts/*/local -Run</c>) в него не попадает: его <c>/metrics</c>
/// отдаётся, но никто его не опрашивает, и графики в Grafana пусты.
/// </remarks>
public interface IDevPrometheusTargetRegistrar
{
    /// <summary>
    /// Объявить цель. Вызывается при старте сервиса; ничего не делает, если
    /// сервис запущен не на хосте.
    /// </summary>
    void Register();
}
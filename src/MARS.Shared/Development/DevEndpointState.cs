namespace MARS.Shared.Development;

/// <summary>Ответ пробы адреса инфраструктуры.</summary>
public enum DevEndpointState
{
    /// <summary>Адрес отвечает: инфраструктура уже поднята.</summary>
    Listening,

    /// <summary>Адрес не отвечает: соединение отвергнуто или истёк таймаут.</summary>
    NotListening,

    /// <summary>
    /// Проверить нечем: адрес не разобран или пуст. Такое считается «поднимать» —
    /// compose идемпотентен, а пропущенная база ломает запуск сервиса.
    /// </summary>
    Unknown,
}

namespace MARS.Shared.Contracts.Messaging;

/// <summary>
/// Связывает контракт интеграционного сообщения с типом его обработчика.
/// Инфраструктурная реализация Listener использует этот маркер, чтобы
/// Shared.Contracts мог вычислять имена очередей без ссылки на Infrastructure.
/// </summary>
public interface IMarsMessageListener<TMessage>
    where TMessage : IMarsEventMessage;

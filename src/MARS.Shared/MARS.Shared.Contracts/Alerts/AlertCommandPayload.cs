using System.Text.Json.Serialization;

namespace MARS.Shared.Contracts.Alerts;

/// <summary>
/// Базовый параметр вызова обработчика алерта из команды. Виртуальный метод
/// <c>InvokeFromCommandAsync</c> в базовом классе обработчиков принимает этот
/// тип; конкретные обработчики переопределяют его и читают свой наследник.
/// Сериализация — полиморфная: дискриминатор <c>$type</c> определяет конкретный
/// payload на стороне MARS.Alerts.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AdhdAlertCommandPayload), "adhd")]
[JsonDerivedType(typeof(MikubeamAlertCommandPayload), "mikubeam")]
public abstract record AlertCommandPayload;

/// <summary>
/// Активация ADHD-эффекта. Пустое <see cref="Seconds"/> (или 0) — переключение
/// в перманентный режим.
/// </summary>
public sealed record AdhdAlertCommandPayload(int Seconds) : AlertCommandPayload;

/// <summary>
/// Досрочная активация MIKU MIKU BEAM.
/// </summary>
public sealed record MikubeamAlertCommandPayload : AlertCommandPayload;

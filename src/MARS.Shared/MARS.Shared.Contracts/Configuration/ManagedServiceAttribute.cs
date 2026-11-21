namespace MARS.Shared.Contracts.Configuration;

/// <summary>
/// Описание сервиса, endpoint которого объявлен свойством <see cref="ServiceEndpoints"/>.
/// Атрибут на самом свойстве — единственное место, где задаются имя сервиса в
/// админке, отображаемое имя и описание; реестр строится из этих аннотаций.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ManagedServiceAttribute(string name, string displayName, string description)
    : Attribute
{
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public string Description { get; } = description;
}

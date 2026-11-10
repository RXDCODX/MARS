namespace MARS.Admin.Api.Contracts;

public class SetEnvironmentVariableRequest
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}

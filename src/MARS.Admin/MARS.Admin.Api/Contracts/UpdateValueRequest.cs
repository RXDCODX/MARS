namespace MARS.Admin.Api.Contracts;

public record UpdateValueRequest
{
    public required string Value { get; init; }
}

namespace MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;

public interface IServiceIdentity
{
    string LogicalName { get; }

    string CanonicalName { get; }

    bool IsSelf(string serviceKey);
}

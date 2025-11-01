namespace SmallHR.Core.Interfaces;

public interface IConnectionResolver
{
    string GetConnectionString(string tenantId);
}



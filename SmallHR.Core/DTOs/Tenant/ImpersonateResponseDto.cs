namespace SmallHR.Core.DTOs.Tenant;

public class ImpersonateResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string ImpersonationToken { get; set; } = string.Empty;
    public ImpersonateTenantDto Tenant { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string Banner { get; set; } = string.Empty;
}

public class ImpersonateTenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
}


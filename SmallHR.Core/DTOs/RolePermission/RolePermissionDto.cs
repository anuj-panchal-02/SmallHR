namespace SmallHR.Core.DTOs.RolePermission;

public class RolePermissionDto
{
    public int Id { get; set; }
    public required string RoleName { get; set; }
    public required string PagePath { get; set; }
    public required string PageName { get; set; }
    public bool CanAccess { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public string? Description { get; set; }
}

public class UpdateRolePermissionDto
{
    public required string RoleName { get; set; }
    public required string PagePath { get; set; }
    public bool CanAccess { get; set; }
    public bool? CanView { get; set; }
    public bool? CanCreate { get; set; }
    public bool? CanEdit { get; set; }
    public bool? CanDelete { get; set; }
}

public class BulkUpdateRolePermissionsDto
{
    public required List<UpdateRolePermissionDto> Permissions { get; set; }
}


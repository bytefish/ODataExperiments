namespace ODataFga.Models;

/// <summary>
/// Models the Document Permissions in the system, representing the various levels of access 
/// that users can have to documents.
/// </summary>
[Flags]
public enum DocPermissions 
{ 
    None = 0, 
    Viewer = 1, 
    Editor = 2, 
    Owner = 4, 
    Approver = 8 
}

/// <summary>
/// Provides methods for mapping string representations of permissions to their corresponding 
/// DocPermissions enumeration values.
/// </summary>
public static class PermissionMapper
{
    /// <summary>
    /// Maps a string representation of a permission to the corresponding DocPermissions enum value.
    /// </summary>
    /// <param name="r">String representation of a permission</param>
    /// <returns>The matching DocPermission</returns>
    public static DocPermissions FromString(string r) => r switch
    {
        "viewer" => DocPermissions.Viewer,
        "editor" => DocPermissions.Editor,
        "owner" => DocPermissions.Owner,
        "approver" => DocPermissions.Approver,
        _ => DocPermissions.None
    };
}
using ODataFga.Fga;

namespace ODataFga.Models;

[FgaType("group")]
public class Group : ISecuredResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = "New Group";

    public string? ParentId { get; set; } // For nested groups

    public List<string> AncestorIds { get; set; } = [];

    public List<PermissionIndex> Permissions { get; set; } = [];
}
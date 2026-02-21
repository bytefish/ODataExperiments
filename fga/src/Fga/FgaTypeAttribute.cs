using ODataFga.Models;

namespace ODataFga.Fga
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FgaTypeAttribute : Attribute { public string Name { get; } public FgaTypeAttribute(string name) => Name = name; }

    public interface ISecuredResource
    {
        string Id { get; set; }
        
        string? ParentId { get; set; }
        
        List<string> AncestorIds { get; set; }
        
        List<PermissionIndex> Permissions { get; set; }
    }
}

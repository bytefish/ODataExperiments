using System.Reflection;

namespace ODataFga.Fga
{
    public static class FgaTypeRegistry
    {
        public static string[] GetTrackedTypes() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ISecuredResource).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => t.GetCustomAttribute<FgaTypeAttribute>()?.Name)
            .Where(name => name != null)
            .Distinct()
            .ToArray()!;

        public static string[] GetTrackedRelations(string objectType) => objectType switch
        {
            "group" => new[] { "owner", "member" },
            "folder" => new[] { "viewer" }, // Ordner haben in unserem Modell nur viewer
            "document" => new[] { "owner", "editor", "viewer" },
            _ => Array.Empty<string>()
        };

        public static string[] GetAllRelations() => new[] { "viewer", "editor", "owner", "member" };
    }
}
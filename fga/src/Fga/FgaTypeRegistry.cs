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

        public static string[] GetTrackedRelations() => new[] { "viewer", "editor", "owner" };
    }
}

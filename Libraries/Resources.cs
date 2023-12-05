using BepInEx;
using System.Reflection;
using System.IO;
using System.Linq;

namespace DojoArenaKT;
public static class Resources
{
    public static string GetString(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            string filePath = assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(resourceName), "");

            if (filePath.IsNullOrWhiteSpace()) return string.Empty;
            using (var stream = assembly.GetManifestResourceStream(filePath))
            {
                using (StreamReader SR = new(stream))
                {
                    return SR.ReadToEnd();
                }
            }
        }
        catch
        {
            return $"Error loading resource {resourceName}";
        }
    }
}
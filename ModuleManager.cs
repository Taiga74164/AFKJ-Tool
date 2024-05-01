using System.Reflection;

namespace AFK_Mod;

public static class ModuleManager
{
    /// <summary>
    /// Loads all modules in the 'AFK_Mod' namespace.
    /// </summary>
    public static void LoadAllModules()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            if (type.Namespace != "AFK_Mod.Modules") continue;
            if (type.BaseType != typeof(Module)) continue;

            var method = type.GetMethod("Load");
            method?.Invoke(null, null);
        }
    }

    /// <summary>
    /// Calls the 'Update' method on all modules in the 'AFK_Mod' namespace.
    /// </summary>
    public static void UpdateAllModules()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            if (type.Namespace != "AFK_Mod.Modules") continue;
            if (type.BaseType != typeof(Module)) continue;

            var method = type.GetMethod("Update");
            method?.Invoke(null, null);
        }
    }
}
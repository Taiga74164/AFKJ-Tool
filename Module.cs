namespace AFK_Mod;

public abstract class Module
{
    /// <summary>
    /// Called when the module is loaded.
    /// </summary>
    public abstract void Load();
    
    /// <summary>
    /// Called every frame.
    /// </summary>
    public abstract void Update();
}
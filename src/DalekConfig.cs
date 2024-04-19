using BepInEx.Configuration;

namespace LethalCompanyDalek;

public class DalekConfig : SyncedInstance<DalekConfig>
{
    public DalekConfig(ConfigFile cfg)
    {
        InitInstance(this);
        
        // Add configs here
        
        ClearUnusedEntries(cfg);
    }
}
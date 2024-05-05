using BepInEx.Configuration;

namespace LethalCompanyDalek;

public class DalekConfig : SyncedInstance<DalekConfig>
{
    public readonly ConfigEntry<int> DalekLazerGunMinValue;
    public readonly ConfigEntry<int> DalekLazerGunMaxValue;
    
    public DalekConfig(ConfigFile cfg)
    {
        InitInstance(this);

        DalekLazerGunMinValue = cfg.Bind(
            "Item Spawn Values",
            "Dalek Gunstick Minimum Value",
            300,
            "The minimum value that a dalek gunstick can spawn with"
            );
        
        DalekLazerGunMaxValue = cfg.Bind(
            "Item Spawn Values",
            "Dalek Gunstick Maximum Value",
            600,
            "The maximum value that a dalek gunstick can spawn with"
        );
    }
}
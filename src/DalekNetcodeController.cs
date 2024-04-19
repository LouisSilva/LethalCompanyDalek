using System;
using BepInEx.Logging;
using Unity.Netcode;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class DalekNetcodeController : NetworkBehaviour
{
    private ManualLogSource _mls;
    
    public event Action<string> OnUpdateDalekId;
    public event Action<string> OnInitializeConfigValues;
    public event Action<string, int> OnDoAnimation;
    public event Action<string, ulong> OnChangeTargetPlayer;
    public event Action<string> OnIncreaseTargetPlayerFearLevel;

    private void Start()
    {
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek Netcode Controller");
    }
    
    [ClientRpc]
    public void ChangeTargetPlayerClientRpc(string receivedDalekId, ulong playerClientId)
    {
        OnChangeTargetPlayer?.Invoke(receivedDalekId, playerClientId);
    }
    
    [ClientRpc]
    public void IncreaseTargetPlayerFearLevelClientRpc(string receivedDalekId)
    {
        OnIncreaseTargetPlayerFearLevel?.Invoke(receivedDalekId);
    }
    
    [ClientRpc]
    public void InitializeConfigValuesClientRpc(string receivedDalekId)
    {
        OnInitializeConfigValues?.Invoke(receivedDalekId);
    }
    
    /// <summary>
    /// Invokes the update aloe id event
    /// </summary>
    /// <param name="receivedDalekId"></param>
    [ClientRpc]
    public void UpdateDalekIdClientRpc(string receivedDalekId)
    {
        OnUpdateDalekId?.Invoke(receivedDalekId);
    }
    
    /// <summary>
    /// Only logs the given message if the assembly version is in debug, not release
    /// </summary>
    /// <param name="msg">The debug message to log</param>
    private void LogDebug(string msg)
    {
        #if DEBUG
        _mls?.LogInfo(msg);
        #endif
    }
}
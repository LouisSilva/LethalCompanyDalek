using System;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class DalekNetcodeController : NetworkBehaviour
{
    private ManualLogSource _mls;
    
    public event Action<string> OnSyncDalekId;
    public event Action<string> OnInitializeConfigValues;
    public event Action<string, int> OnDoAnimation;
    public event Action<string, ulong> OnChangeTargetPlayer;
    public event Action<string> OnIncreaseTargetPlayerFearLevel;
    public event Action<string> OnShootGun;
    public event Action<string> OnEnterDeathState;
    public event Action<string, NetworkObjectReference, int> OnSpawnDalekLazerGun;
    public event Action<string> OnGrabDalekLazerGun;

    private void Start()
    {
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek Netcode Controller");
    }

    [ServerRpc]
    public void SpawnDalekLazerGunServerRpc(string receivedDalekId)
    {
        GameObject dalekLazerGunObject = Instantiate(
            DalekPlugin.dalekLazerGun.spawnPrefab,
            transform.position,
            Quaternion.identity,
            RoundManager.Instance.spawnedScrapContainer);

        int dalekLazerGunScrapValue = UnityEngine.Random.Range(
            DalekConfig.Instance.DalekLazerGunMinValue.Value,
            DalekConfig.Instance.DalekLazerGunMaxValue.Value);

        dalekLazerGunObject.GetComponent<GrabbableObject>().fallTime = 0f;
        dalekLazerGunObject.GetComponent<GrabbableObject>().SetScrapValue(dalekLazerGunScrapValue);
        RoundManager.Instance.totalScrapValueInLevel += dalekLazerGunScrapValue;
        
        dalekLazerGunObject.GetComponent<NetworkObject>().Spawn();
        SpawnDalekLazerGunClientRpc(receivedDalekId, dalekLazerGunObject, dalekLazerGunScrapValue);
    }

    [ClientRpc]
    private void SpawnDalekLazerGunClientRpc(string receivedDalekId, NetworkObjectReference dalekLazerGunObject,
        int dalekLazerGunScrapValue)
    {
        OnSpawnDalekLazerGun?.Invoke(receivedDalekId, dalekLazerGunObject, dalekLazerGunScrapValue);
    }

    [ClientRpc]
    public void GrabDalekLazerGunClientRpc(string receivedDalekId)
    {
        OnGrabDalekLazerGun?.Invoke(receivedDalekId);
    }
    
    [ClientRpc]
    public void EnterDeathStateClientRpc(string receivedDalekId)
    {
        OnEnterDeathState?.Invoke(receivedDalekId);
    }
    
    [ClientRpc]
    public void ShootGunClientRpc(string receivedDalekId)
    {
        OnShootGun?.Invoke(receivedDalekId);
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
    public void SyncDalekIdClientRpc(string receivedDalekId)
    {
        OnSyncDalekId?.Invoke(receivedDalekId);
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
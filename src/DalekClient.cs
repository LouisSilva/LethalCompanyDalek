using System;
using System.Collections;
using BepInEx.Logging;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class DalekClient : MonoBehaviour
{
    private ManualLogSource _mls;
    private string _dalekId;
    
#pragma warning disable 0649
    [Header("Audio")] [Space(5f)] 
    [SerializeField] private AudioSource dalekVoiceSource;
    [SerializeField] private AudioSource dalekSfxSource;
    
    public AudioClip[] sawPlayerAudio;
    public AudioClip[] roamingAudio;
    public AudioClip[] killedPlayerAudio;
    public AudioClip[] exterminateAudio;
    public AudioClip[] disabledShipAudio;
    
    [Header("Controllers")] [Space(5f)] 
    [SerializeField] private Transform dalekLazerGunBone;
    [SerializeField] private Animator animator;
    [SerializeField] private DalekNetcodeController netcodeController;
#pragma warning restore 0649

    private PlayerControllerB _targetPlayer;

    private NetworkObjectReference _dalekLazerGunObjectRef;

    private DalekLazerItem _heldDalekLazerGun;

    private int _dalekLazerGunScrapValue;

    private void OnEnable()
    {
        netcodeController.OnSyncDalekId += HandleSyncDalekId;
        netcodeController.OnInitializeConfigValues += HandleInitializeConfigValues;
        netcodeController.OnChangeTargetPlayer += HandleChangeTargetPlayer;
        netcodeController.OnSpawnDalekLazerGun += HandleSpawnDalekLazerGun;
        netcodeController.OnGrabDalekLazerGun += HandleGrabDalekLazerGun;
        netcodeController.OnShootGun += HandleShootDalekLazerGun;
        netcodeController.OnEnterDeathState += HandleEnterDeathState;
        netcodeController.OnPlayAudioClipType += HandlePlayAudioClipType;
    }

    private void OnDisable()
    {
        netcodeController.OnSyncDalekId -= HandleSyncDalekId;
        netcodeController.OnInitializeConfigValues -= HandleInitializeConfigValues;
        netcodeController.OnChangeTargetPlayer -= HandleChangeTargetPlayer;
        netcodeController.OnSpawnDalekLazerGun -= HandleSpawnDalekLazerGun;
        netcodeController.OnGrabDalekLazerGun -= HandleGrabDalekLazerGun;
        netcodeController.OnShootGun -= HandleShootDalekLazerGun;
        netcodeController.OnEnterDeathState -= HandleEnterDeathState;
        netcodeController.OnPlayAudioClipType -= HandlePlayAudioClipType;
    }

    private void Awake()
    {
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek Client {_dalekId}");
    }

    private void HandleShootDalekLazerGun(string receivedDalekId)
    {
        if (_dalekId != receivedDalekId) return;
        StartCoroutine(ShootLazerGun());
    }

    private IEnumerator ShootLazerGun()
    {
        _heldDalekLazerGun.isBeingUsed = true;
        yield return new WaitForSeconds(0.75f);
        _heldDalekLazerGun.isBeingUsed = false;
    }

    private void HandleSpawnDalekLazerGun(string receivedDalekId, NetworkObjectReference dalekLazerGunObjectReference,
        int dalekLazerGunScrapValue)
    {
        if (_dalekId != receivedDalekId) return;
        _dalekLazerGunObjectRef = dalekLazerGunObjectReference;
        _dalekLazerGunScrapValue = dalekLazerGunScrapValue;
    }

    private void HandleGrabDalekLazerGun(string receivedDalekId)
    {
        if (_dalekId != receivedDalekId) return;
        if (_heldDalekLazerGun != null) return;
        if (!_dalekLazerGunObjectRef.TryGet(out NetworkObject networkObject)) return;
        _heldDalekLazerGun = networkObject.gameObject.GetComponent<DalekLazerItem>();
        
        _heldDalekLazerGun.SetScrapValue(_dalekLazerGunScrapValue);
        _heldDalekLazerGun.parentObject = dalekLazerGunBone;
        _heldDalekLazerGun.isHeldByEnemy = true;
        _heldDalekLazerGun.grabbableToEnemies = false;
        _heldDalekLazerGun.grabbable = false;
    }

    private void HandlePlayAudioClipType(
        string receivedDalekId, 
        DalekNetcodeController.AudioClipTypes audioClipType, 
        int clipIndex, 
        bool interrupt = true)
    {
        if (_dalekId != receivedDalekId) return;

        AudioClip audioClipToPlay = audioClipType switch
        {
            DalekNetcodeController.AudioClipTypes.SawPlayer => sawPlayerAudio[clipIndex],
            DalekNetcodeController.AudioClipTypes.Roaming => roamingAudio[clipIndex],
            DalekNetcodeController.AudioClipTypes.KilledPlayer => killedPlayerAudio[clipIndex],
            DalekNetcodeController.AudioClipTypes.Exterminate => exterminateAudio[clipIndex],
            DalekNetcodeController.AudioClipTypes.DisabledShip => disabledShipAudio[clipIndex],
            _ => null
        };

        if (audioClipToPlay == null)
        {
            _mls.LogError($"Invalid audio clip with type: {audioClipType} and index: {clipIndex}");
            return;
        }
        
        LogDebug($"Playing audio clip: {audioClipToPlay.name}");
        if (interrupt) dalekVoiceSource.Stop(true);
        dalekVoiceSource.PlayOneShot(audioClipToPlay);
        WalkieTalkie.TransmitOneShotAudio(dalekVoiceSource, audioClipToPlay, dalekVoiceSource.volume);
    }
    
    
    /// <summary>
    /// Changes the target player to the player with the given playerObjectId.
    /// </summary>
    /// <param name="receivedDalekId">The dalek id</param>
    /// <param name="targetPlayerObjectId">The target player's object ID</param>
    private void HandleChangeTargetPlayer(string receivedDalekId, ulong targetPlayerObjectId)
    {
        if (_dalekId != receivedDalekId) return;
        if (targetPlayerObjectId == 69420)
        {
            _targetPlayer = null;
            return;
        }
        
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[targetPlayerObjectId];
        _targetPlayer = player;
    }
    
    /// <summary>
    /// Handles what happens when the dalek is dead.
    /// </summary>
    /// <param name="receivedDalekId">The dalek id</param>
    private void HandleEnterDeathState(string receivedDalekId)
    {
        if (_dalekId != receivedDalekId) return;

        if (_heldDalekLazerGun != null)
        {
            _heldDalekLazerGun.grabbableToEnemies = false;
            _heldDalekLazerGun.grabbable = true;
        }
        
        Destroy(this);
    }
    
    /// <summary>
    /// Sets the configurable variables to their value in the player's config
    /// </summary>
    /// <param name="receivedDalekId">The dalek id</param>
    private void HandleInitializeConfigValues(string receivedDalekId)
    {
        if (_dalekId != receivedDalekId) return;
    }
    
    /// <summary>
    /// Syncs the Dalek id with the server
    /// </summary>
    /// <param name="id">The dalek id</param>
    private void HandleSyncDalekId(string id)
    {
        _dalekId = id;
        _mls?.Dispose();
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek Client {_dalekId}");
        
        LogDebug("Successfully synced dalek id");
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
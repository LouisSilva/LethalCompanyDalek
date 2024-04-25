using System;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class DalekLaserItem : PhysicsProp
{
    [Serializable]
    public struct ItemOffset : INetworkSerializable
    {
        public Vector3 positionOffset = default;
        public Vector3 rotationOffset = default;
    
        public ItemOffset(Vector3 positionOffset = default, Vector3 rotationOffset = default)
        {
            this.positionOffset = positionOffset;
            this.rotationOffset = rotationOffset;
        }
    
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref positionOffset);
            serializer.SerializeValue(ref rotationOffset);
        }
    }
    
    private ManualLogSource _mls;
    private string _laserId;
    
    [SerializeField] private GameObject laserPrefab;
    private GameObject _currentLaser;
    private LaserBeamBehaviour _currentLaserBeam;
    public bool isTriggerHeld = false;
    
    [SerializeField] private ItemOffset playerLaserOffset;
    [SerializeField] private ItemOffset enemyLaserOffset;

    private void Awake()
    {
        playerLaserOffset = new ItemOffset();
        enemyLaserOffset = new ItemOffset();
    }

    public override void Start()
    {
        base.Start();

        _laserId = Guid.NewGuid().ToString();
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek Laser {_laserId}");
    }

    public override void Update()
    {
        base.Update();
        if (OwnerClientId != GameNetworkManager.Instance.localPlayerController.playerClientId) return;
        
        isTriggerHeld = Keyboard.current.spaceKey.isPressed;
        if (isTriggerHeld)
        {
            _currentLaser = Instantiate(laserPrefab, transform.position, Quaternion.identity);
            _currentLaser.transform.localRotation = transform.rotation * Quaternion.Euler(-90, 0, 0);
            _currentLaserBeam = _currentLaser.GetComponent<LaserBeamBehaviour>();
            _currentLaserBeam.StartFiring(this, transform);
        }
        else
        {
            if (_currentLaserBeam == null) return;
            _currentLaserBeam.StopFiring();
            _currentLaser = null;
            _currentLaserBeam = null;
        }
    }
    
    public override void LateUpdate()
    {
        if (parentObject != null)
        {
            Vector3 rotationOffset;
            Vector3 positionOffset;
            
            if (isHeldByEnemy)
            {
                rotationOffset = enemyLaserOffset.rotationOffset;
                positionOffset = enemyLaserOffset.positionOffset;
            }
            else
            {
                rotationOffset = playerLaserOffset.rotationOffset;
                positionOffset = playerLaserOffset.positionOffset;
            }
            
            transform.rotation = parentObject.rotation;
            transform.Rotate(rotationOffset);
            transform.position = parentObject.position;
            transform.position += parentObject.rotation * positionOffset;
            
        }
        if (!(radarIcon != null)) return;
        radarIcon.position = transform.position;
    }
    
    private void LogDebug(string msg)
    {
        #if DEBUG
        if (!IsOwner) return;
        _mls.LogInfo(msg);
        #endif
    }
}
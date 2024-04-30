using System;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;
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
    
    [SerializeField] private GameObject laserBeamPrefab;

    [SerializeField] private Transform lazerShootPoint;
    
    [HideInInspector] public bool isTriggerHeld;
    
    [SerializeField] private ItemOffset playerLaserItemOffset;
    [SerializeField] private ItemOffset enemyLaserItemOffset;
    
    private GameObject _currentLaser;
    private LaserBeamBehaviour _currentLaserBeam;

    private void Awake()
    {
        playerLaserItemOffset = new ItemOffset();
        enemyLaserItemOffset = new ItemOffset();
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
        if (!IsOwner) return;
        
        if (isBeingUsed)
        {
            _currentLaser = Instantiate(laserBeamPrefab, lazerShootPoint.position, Quaternion.identity);
            _currentLaser.transform.localRotation = lazerShootPoint.rotation * Quaternion.Euler(-90, 0, 0);
            _currentLaserBeam = _currentLaser.GetComponent<LaserBeamBehaviour>();
            
            if (isHeld && !isHeldByEnemy) _currentLaserBeam.StartFiring(this, lazerShootPoint, playerHeldBy);
            else _currentLaserBeam.StartFiring(this, lazerShootPoint);
        }
        
        else
        {
            if (_currentLaserBeam == null) return;
            _currentLaserBeam.StopFiring();
            _currentLaser = null;
            _currentLaserBeam = null;
        }
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        isBeingUsed = buttonDown;
    }
    
    public override void LateUpdate()
    {
        if (parentObject != null)
        {
            Vector3 rotationOffset;
            Vector3 positionOffset;
            
            if (isHeldByEnemy)
            {
                rotationOffset = enemyLaserItemOffset.rotationOffset;
                positionOffset = enemyLaserItemOffset.positionOffset;
            }
            else
            {
                rotationOffset = playerLaserItemOffset.rotationOffset;
                positionOffset = playerLaserItemOffset.positionOffset;
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
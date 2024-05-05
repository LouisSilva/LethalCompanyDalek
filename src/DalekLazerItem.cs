using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Unity.Netcode;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class DalekLazerItem : PhysicsProp
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
    private string _lazerId;
    
    [SerializeField] private GameObject lazerBeamPrefab;
    
    [SerializeField] private Transform lazerShootPoint;
    
    [SerializeField] private ItemOffset playerlazerItemOffset;
    [SerializeField] private ItemOffset enemylazerItemOffset;

    [SerializeField] private int amountOfLazersToPool = 200;

    private List<GameObject> _pooledLazerObjects;

    private void Awake()
    {
        _lazerId = Guid.NewGuid().ToString();
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek lazer {_lazerId}");
        
        playerlazerItemOffset = new ItemOffset();
        enemylazerItemOffset = new ItemOffset();
    }

    public override void Start()
    {
        base.Start();

        // Create a pool of lazer beams to be used when the gun is fired
        // This improves performance significantly, because instead of constantly initializing and destroying gameobjects, it recycles them
        _pooledLazerObjects = [];
        for (int i = 0; i < amountOfLazersToPool; i++)
        {
            GameObject tmp = Instantiate(lazerBeamPrefab);
            tmp.SetActive(false);
            _pooledLazerObjects.Add(tmp);
        }
    }

    public override void Update()
    {
        base.Update();
        if (!IsOwner) return;
        if (!isBeingUsed) return;

        // Get pooled lazer beam object
        GameObject currentlazerBeamObject = GetPooledLazerBeamObject();
        if (currentlazerBeamObject == null) return;

        // Set the position and rotation
        currentlazerBeamObject.transform.position = lazerShootPoint.transform.position;
        currentlazerBeamObject.transform.rotation = Quaternion.identity;
        currentlazerBeamObject.transform.localRotation = lazerShootPoint.rotation * Quaternion.Euler(-90, 0, 0);
        
        // Tell the lazer beam to start moving
        LazerBeamBehaviour currentLazerBeamBehaviour = currentlazerBeamObject.GetComponent<LazerBeamBehaviour>();
        if (isHeld && !isHeldByEnemy) currentLazerBeamBehaviour.StartFiring(this, lazerShootPoint, playerHeldBy);
        else  currentLazerBeamBehaviour.StartFiring(this, lazerShootPoint);
    }

    private GameObject GetPooledLazerBeamObject()
    {
        return _pooledLazerObjects.FirstOrDefault(lazerBeam => !lazerBeam.activeInHierarchy);
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
                rotationOffset = enemylazerItemOffset.rotationOffset;
                positionOffset = enemylazerItemOffset.positionOffset;
            }
            else
            {
                rotationOffset = playerlazerItemOffset.rotationOffset;
                positionOffset = playerlazerItemOffset.positionOffset;
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
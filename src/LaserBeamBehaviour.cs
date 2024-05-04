﻿using System;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class LaserBeamBehaviour : MonoBehaviour
{
    private string _lazerBeamId;
    private ManualLogSource _mls;
    
    [SerializeField] private float speed = 300f; // Speed of the laser
    [SerializeField] private float maxStretch = 300f; // Maximum length of the laser beam
    [SerializeField] private float maxAirTime = 4f;

#pragma warning disable 0649
    [SerializeField] private Renderer frontSemicircleRenderer;
    [SerializeField] private Renderer backSemicircleRenderer;

    [SerializeField] private Transform frontSemicircleTransform;
    [SerializeField] private Transform backSemicircleTransform;
#pragma warning restore 0649

    private float _timeAlive;
    private float _currentLength; 
    private float _hitCooldown;
    
    public bool triggerHeld;

    private PlayerControllerB _playerShotFrom;
    
    private Vector3 _gunTransformForward;
    private Vector3 _origionalSemicircleScale;
    
    private DalekLaserItem _gunFiredFrom;

    private void Awake()
    {
        _lazerBeamId = Guid.NewGuid().ToString();
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Lazer Beam {_lazerBeamId}");
    }

    private void OnDestroy()
    {
        if (_playerShotFrom == null) return;
        _playerShotFrom.playerActions.Movement.Look.performed -= HandleStopFiring;
    }

    private void Start()
    {
        _origionalSemicircleScale = frontSemicircleTransform.localScale;
        frontSemicircleRenderer.enabled = true;

        speed = 300f;
        maxAirTime = 4f;
    }

    private void Update()
    {
        _hitCooldown -= Time.deltaTime;
        
        if (triggerHeld)
        {
            LogDebug("Trigger is held");
            if (_currentLength < maxStretch)
            {
                // Increase the length of the laser if the trigger is held
                float increment = speed * Time.deltaTime;
                _currentLength += increment;
                _currentLength = Mathf.Min(_currentLength, maxStretch);
                transform.localScale = new Vector3(transform.localScale.x, _currentLength, transform.localScale.z);

                // Fix the semicircle scales
                frontSemicircleTransform.localScale = new Vector3(_origionalSemicircleScale.x,
                    _origionalSemicircleScale.y / transform.localScale.y, _origionalSemicircleScale.z);

                backSemicircleTransform.localScale = new Vector3(_origionalSemicircleScale.x,
                    _origionalSemicircleScale.y / transform.localScale.y, _origionalSemicircleScale.z);

                _timeAlive = 0;
            }
            
            backSemicircleRenderer.enabled = false;
        }
        else backSemicircleRenderer.enabled = true;
        
        // move the laser forward
        transform.position += _gunTransformForward * (speed * Time.deltaTime);

        _timeAlive += Time.deltaTime;
        if (_timeAlive > maxAirTime) Destroy(gameObject);
    }

    public void StartFiring(DalekLaserItem gunFiredFromLocal, Transform gunTransform = default, PlayerControllerB playerShotFrom = null)
    {
        _gunFiredFrom = gunFiredFromLocal;
        if (gunTransform != null)
        {
            _gunTransformForward = gunTransform.forward;
        }
        else
        {
            LogDebug("Gun transform is null my g");
            _gunTransformForward = default;
        }
            
        triggerHeld = true;
        _currentLength = 0;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        
        // Subscribe to the event when the player moves their mouse
        if (playerShotFrom == null) return;
        _playerShotFrom = playerShotFrom;
        _playerShotFrom.playerActions.Movement.Look.performed += HandleStopFiring;
    }

    public void StopFiring()
    {
        triggerHeld = false;
        LogDebug("Stop firing called");
    }

    private void HandleStopFiring(InputAction.CallbackContext context)
    {
        StopFiring();
    }

    private void OnTriggerStay(Collider other)
    {
        if (_hitCooldown > 0) return;
        if (!other.TryGetComponent(out IHittable hittable)) return;
        hittable.Hit(9999, Vector3.zero, _playerShotFrom, false, 731);
        _hitCooldown = 0.25f;
    }

    private void LogDebug(string msg)
    {
        #if DEBUG
        _mls.LogInfo($"{msg}");
        #endif
    }
}
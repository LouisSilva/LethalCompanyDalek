using System;
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
    private float _hitCooldown;
    
    private Vector3 _gunTransformForward;

    private PlayerControllerB _playerShotFrom;
    
    private DalekLaserItem _gunFiredFrom;

    private void Awake()
    {
        _lazerBeamId = Guid.NewGuid().ToString();
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Lazer Beam {_lazerBeamId}");
    }

    private void Start()
    {
        frontSemicircleRenderer.enabled = true;
        backSemicircleRenderer.enabled = true;

        speed = 25f;
        maxAirTime = 4f;
    }

    private void Update()
    {
        _hitCooldown -= Time.deltaTime;
        
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
            LogDebug("Gun transform is null");
            _gunTransformForward = default;
        }

        _playerShotFrom = playerShotFrom;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    public void StopFiring()
    {
        
    }

    private void HandleStopFiring(InputAction.CallbackContext context)
    {
        StopFiring();
    }

    private void OnTriggerStay(Collider other)
    {
        if (_hitCooldown > 0) return;
        if (!other.TryGetComponent(out IHittable hittable)) return;
        hittable.Hit(100, Vector3.zero, _playerShotFrom, false, 731);
        _hitCooldown = 0.25f;
    }

    private void LogDebug(string msg)
    {
        #if DEBUG
        _mls.LogInfo($"{msg}");
        #endif
    }
}
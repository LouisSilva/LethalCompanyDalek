using System;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class LazerBeamBehaviour : MonoBehaviour
{
    private string _lazerBeamId;
    private ManualLogSource _mls;
    
    [SerializeField] private float speed = 300f; // Speed of the laser
    [SerializeField] private float maxAirTime = 4f;

#pragma warning disable 0649
    [SerializeField] private Renderer frontSemicircleRenderer;
    [SerializeField] private Renderer backSemicircleRenderer;
#pragma warning restore 0649
    
    private float _hitCooldown;
    
    private Vector3 _gunTransformForward;

    private PlayerControllerB _playerShotFrom;
    
    private DalekLazerItem _gunFiredFrom;

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

    private void OnEnable()
    {
        // Deactivate game object after the max allowed air time
        Invoke(nameof(Deactivate), maxAirTime);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Deactivate));
    }

    private void Update()
    {
        _hitCooldown -= Time.deltaTime;
        
        // move the laser forward
        transform.position += _gunTransformForward * (speed * Time.deltaTime);
    }

    public void StartFiring(DalekLazerItem gunFiredFromLocal, Transform gunTransform = default, PlayerControllerB playerShotFrom = null)
    {
        gameObject.SetActive(true);
        _gunFiredFrom = gunFiredFromLocal;
        _gunTransformForward = gunTransform != null ? gunTransform.forward : default;
        _playerShotFrom = playerShotFrom;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
        
        // Reset other variables because the game objects are recycled
        _hitCooldown = 0;
        _gunTransformForward = default;
        _playerShotFrom = null;
        _gunFiredFrom = null;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_hitCooldown <= 0)
        {
            if (other.TryGetComponent(out EnemyAI enemyAI)) enemyAI.HitEnemy(100, _playerShotFrom, false, 731);
            else if (other.TryGetComponent(out PlayerControllerB player)) player.DamagePlayer(100, false, true, CauseOfDeath.Gunshots);
        }
        
        _hitCooldown = 0.25f;
    }

    private void LogDebug(string msg)
    {
        #if DEBUG
        _mls.LogInfo($"{msg}");
        #endif
    }
}
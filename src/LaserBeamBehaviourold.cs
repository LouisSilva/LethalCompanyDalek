using System;
using GameNetcodeStuff;
using UnityEngine;

namespace LethalCompanyDalek;

public class LaserBeamBehaviou : MonoBehaviour
{
    [SerializeField] private float speed = 50f; // Speed of the laser
    [SerializeField] private float maxStretch = 300f; // Maximum length of the laser beam
    [SerializeField] private float maxAirTime = 10f;

#pragma warning disable 0649
    [SerializeField] private Renderer frontSemicircleRenderer;
    [SerializeField] private Renderer backSemicircleRenderer;

    [SerializeField] private Transform frontSemicircleTransform;
    [SerializeField] private Transform backSemicircleTransform;
#pragma warning restore 0649

    private float _timeAlive;
    private float _currentLength; // Current length of the laser
    private float _oldPlayerCameraUp;
    private float _oldPlayerYRotation;
    
    public bool triggerHeld; // Whether the trigger is still held

    private PlayerControllerB _playerShotFrom;
    
    private Vector3 _gunTransformForward;
    private Vector3 _origionalSemicircleScale;

    private FiringMode _currentFiringMode;
    
    private DalekLaserItem _gunFiredFrom;

    private enum FiringMode
    {
        Dalek,
        Player,
    }

    private void Start()
    {
        _origionalSemicircleScale = frontSemicircleTransform.localScale;
        frontSemicircleRenderer.enabled = true;
    }

    private void Update()
    {
        switch (_currentFiringMode)
        {
            case FiringMode.Player:
            {
                if (!_gunFiredFrom.isTriggerHeld || CheckIfCurrentPlayerViewHasChanged())
                {
                    Debug.Log("TRIGGER IS NOT HELD");
                    triggerHeld = false;
                }
                
                _oldPlayerCameraUp = _playerShotFrom.cameraUp;
                _oldPlayerYRotation = _playerShotFrom.thisPlayerBody.eulerAngles.y;
                break;
            }
            
            case FiringMode.Dalek:
                break;
            
            default:
                return;
        }

        if (triggerHeld)
        {
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

    public void StartFiring(DalekLaserItem gunFiredFromLocal, PlayerControllerB playerShotFrom, Transform gunTransform = default)
    {
        _gunFiredFrom = gunFiredFromLocal;
        if (gunTransform != null)
        {
            _gunTransformForward = gunTransform.forward;
        }
        else
        {
            Debug.Log("Gun transform is null my g, bad stuff");
            return;
        }
        
        triggerHeld = true;
        _currentLength = 0;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        _playerShotFrom = playerShotFrom;
        _oldPlayerCameraUp = _playerShotFrom.cameraUp;
        _oldPlayerYRotation = _playerShotFrom.thisPlayerBody.eulerAngles.y;
        _currentFiringMode = FiringMode.Player;
    }
    
    public void StartFiring(DalekLaserItem gunFiredFromLocal, Transform gunTransform = default)
    {
        _gunFiredFrom = gunFiredFromLocal;
        if (gunTransform != null)
        {
            _gunTransformForward = gunTransform.forward;
        }
        else
        {
            Debug.Log("Gun transform is null my g, bad stuff");
            return;
        }
        
        triggerHeld = true;
        _currentLength = 0;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        _playerShotFrom = null;
        _currentFiringMode = FiringMode.Dalek;
    }

    public void StopFiring()
    {
        triggerHeld = false;
    }

    private bool CheckIfCurrentPlayerViewHasChanged()
    {
        // Access rotation values from the player controller
        float currentCameraUp = _playerShotFrom.cameraUp;
        float currentYRot = _playerShotFrom.thisPlayerBody.eulerAngles.y;

        // Check if the view has significantly changed
        return Mathf.Abs(_oldPlayerCameraUp - currentCameraUp) > 1.0f || Mathf.Abs(_oldPlayerYRotation - currentYRot) > 1.0f;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out IHittable hittable))
        {
            hittable.Hit(9999, Vector3.zero, _playerShotFrom, false, 731);
        }
    }
}
using GameNetcodeStuff;
using UnityEngine;

namespace LethalCompanyDalek;

public class LaserBeamBehaviour : MonoBehaviour
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
    
    public bool triggerHeld; // Whether the trigger is still held

    private PlayerControllerB _playerShotFrom;
    
    private Vector3 _gunTransformForward;
    private Vector3 _gunTransformPosition;
    private Vector3 _origionalSemicircleScale;
    
    private Quaternion _gunTransformRotation;
    
    private DalekLaserItem _gunFiredFrom;

    private void Start()
    {
        _origionalSemicircleScale = frontSemicircleTransform.localScale;
        frontSemicircleRenderer.enabled = true;
    }

    private void Update()
    {
        if (!_gunFiredFrom.isTriggerHeld || _gunTransformPosition != _gunFiredFrom.transform.position || _gunTransformRotation != _gunFiredFrom.transform.rotation)
        {
            Debug.Log("TRIGGER IS NOT HELD");
            triggerHeld = false;
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

    public void StartFiring(DalekLaserItem gunFiredFromLocal, Transform gunTransform = default, PlayerControllerB playerShotFrom = null)
    {
        _gunFiredFrom = gunFiredFromLocal;
        if (gunTransform != null)
        {
            _gunTransformForward = gunTransform.forward;
            _gunTransformPosition = gunTransform.position;
            _gunTransformRotation = gunTransform.rotation;
        }
        else
        {
            Debug.Log("Gun transform is null my g");
            _gunTransformForward = default;
            _gunTransformPosition = default;
            _gunTransformRotation = default;
        }
            
        triggerHeld = true;
        _currentLength = 0;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        _playerShotFrom = playerShotFrom;
    }

    public void StopFiring()
    {
        triggerHeld = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out IHittable hittable))
        {
            hittable.Hit(9999, Vector3.zero, _playerShotFrom, false, 731);
        }
    }
}
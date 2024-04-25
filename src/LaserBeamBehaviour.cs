using UnityEngine;

namespace LethalCompanyDalek;

public class LaserBeamBehaviour : MonoBehaviour
{
    public float speed = 200f; // Speed of the laser
    public float maxStretch = 300f; // Maximum length of the laser beam
    public float maxAirTime = 10f;

    private float _timeAlive = 0f;
    public bool triggerHeld = false; // Whether the trigger is still held
    private float _currentLength = 0f; // Current length of the laser
        
    private Vector3 _gunTransformForward;
    private Vector3 _gunTransformPosition;
    private Quaternion _gunTransformRotation;
    private CapsuleCollider _laserCollider;

    private DalekLaserItem _gunFiredFrom;

    private void Update()
    {
        if (!_gunFiredFrom.isTriggerHeld || _gunTransformPosition != _gunFiredFrom.transform.position || _gunTransformRotation != _gunFiredFrom.transform.rotation)
        {
            triggerHeld = false;
        }
            
        if (triggerHeld && _currentLength < maxStretch)
        {
            // Increase the length of the laser if the trigger is held
            float increment = speed * Time.deltaTime;
            _currentLength += increment;
            _currentLength = Mathf.Min(_currentLength, maxStretch);
            transform.localScale = new Vector3(transform.localScale.x, _currentLength, transform.localScale.z);
            _timeAlive = 0;
        }
            
        // move the laser forward
        transform.position += _gunTransformForward * (speed * Time.deltaTime);

        _timeAlive += Time.deltaTime;
        if (_timeAlive > maxAirTime) Destroy(gameObject);
    }

    public void StartFiring(DalekLaserItem gunFiredFromLocal, Transform gunTransform = default)
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
            _gunTransformForward = default;
            _gunTransformPosition = default;
            _gunTransformRotation = default;
        }
            
            
        triggerHeld = true;
        _currentLength = 0;
        transform.localScale = new Vector3(transform.localScale.x, 3f, transform.localScale.z);
    }

    public void StopFiring()
    {
        triggerHeld = false;
    }
}
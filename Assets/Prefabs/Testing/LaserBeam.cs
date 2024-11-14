using UnityEngine;

namespace LethalCompany.Mods.Dalek.Prefabs.Testing
{
    public class LaserBeam : MonoBehaviour
    {
        public float speed = 200f; // Speed of the laser
        public float maxStretch = 300f; // Maximum length of the laser beam
        public float maxAirTime = 10f;

        private float timeAlive = 0f;
        public bool triggerHeld = false; // Whether the trigger is still held
        private float currentLength = 0f; // Current length of the laser
        
        private Vector3 gunTransformForward;
        private Vector3 gunTransformPosition;
        private Quaternion gunTransformRotation;
        private CapsuleCollider laserCollider;

        private LaserController gunFiredFrom;

        private void Update()
        {
            if (!gunFiredFrom.isTriggerHeld || gunTransformPosition != gunFiredFrom.transform.position || gunTransformRotation != gunFiredFrom.transform.rotation)
            {
                triggerHeld = false;
            }
            
            if (triggerHeld && currentLength < maxStretch)
            {
                // Increase the length of the laser if the trigger is held
                float increment = speed * Time.deltaTime;
                currentLength += increment;
                currentLength = Mathf.Min(currentLength, maxStretch);
                transform.localScale = new Vector3(transform.localScale.x, currentLength, transform.localScale.z);
                timeAlive = 0;
            }
            
            // move the laser forward
            transform.position += gunTransformForward * (speed * Time.deltaTime);

            timeAlive += Time.deltaTime;
            if (timeAlive > maxAirTime) Destroy(gameObject);
        }

        public void StartFiring(LaserController gunFiredFromLocal, Transform gunTransform = default)
        {
            gunFiredFrom = gunFiredFromLocal;
            if (gunTransform != null)
            {
                gunTransformForward = gunTransform.forward;
                gunTransformPosition = gunTransform.position;
                gunTransformRotation = gunTransform.rotation;
            }
            else
            {
                gunTransformForward = default;
                gunTransformPosition = default;
                gunTransformRotation = default;
            }
            
            
            triggerHeld = true;
            currentLength = 0;
            transform.localScale = new Vector3(transform.localScale.x, 3f, transform.localScale.z);
        }

        public void StopFiring()
        {
            triggerHeld = false;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalCompany.Mods.Dalek.Prefabs.Testing
{
    public class LaserController : MonoBehaviour
    {
        [SerializeField] private GameObject laserPrefab;
        
        private GameObject currentLaser;
        private LaserBeam currentLaserBeam;

        public bool isTriggerHeld = false;

        private void Update()
        {
            isTriggerHeld = Keyboard.current.spaceKey.isPressed;
            
            if (isTriggerHeld) 
            {
                currentLaser = Instantiate(laserPrefab, transform.position, Quaternion.identity);
                currentLaser.transform.localRotation = transform.rotation * Quaternion.Euler(-90, 0, 0);
                currentLaserBeam = currentLaser.GetComponent<LaserBeam>();
                currentLaserBeam.StartFiring(this, transform);
            }
            else
            {
                if (currentLaserBeam == null) return;
                currentLaserBeam.StopFiring();
                currentLaser = null;
                currentLaserBeam = null;
            }
        }
    }
}

using UnityEngine;

namespace LethalCompany.Mods.Dalek.Prefabs.Testing
{
    public class RotateToMouse : MonoBehaviour
    {
        public Camera camera;
        public float maximumLength = float.MaxValue;

        private Ray rayMouse;
        private Vector3 pos;
        private Vector3 direction;
        private Quaternion rotation;

        private void Update()
        {
            if (camera == null) return;

            Vector3 mousePos = Input.mousePosition;
            rayMouse = camera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(rayMouse.origin, rayMouse.direction, out RaycastHit hit, maximumLength))
            {
                RotateMouseToDirection(gameObject, hit.point);
            }
            else
            {
                Vector3 pos1 = rayMouse.GetPoint(maximumLength);
                RotateMouseToDirection(gameObject, pos1);
            }
        }

        private void RotateMouseToDirection(GameObject obj, Vector3 destination)
        {
            direction = destination - obj.transform.position;
            rotation = Quaternion.LookRotation(direction);
            obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, rotation, 1);
        }
    }
}

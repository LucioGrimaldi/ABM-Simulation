using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Camera camera;
    private Vector3 previousPosition;
    void LateUpdate()
    {
        if (Input.GetMouseButton(1))
        {
            transform.eulerAngles += rotationSpeed * new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
        }
    }
}

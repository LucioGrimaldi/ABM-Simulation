using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Camera camera;
    [SerializeField] private Transform target;
    private Vector3 previousPosition;
    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            previousPosition = camera.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 newPosition = camera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;

            float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = direction.y * 180; // camera moves vertically

            transform.position = target.position;

            //transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);

            //camera.transform.LookAt(transform, Vector3.up);
            //camera.transform.Translate(direction * rotationSpeed * Time.deltaTime);
            transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);
            camera.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis * 0.5f, Space.Self);
            //transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis, Space.World);


            previousPosition = newPosition;
        }

    }
}

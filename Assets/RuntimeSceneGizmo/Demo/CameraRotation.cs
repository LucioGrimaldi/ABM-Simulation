using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Camera camera;
    [SerializeField] private Transform target;
    private Vector3 previousPosition;
    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(1))
        {
            previousPosition = camera.ScreenToViewportPoint(Input.mousePosition);
        }
        else if (Input.GetMouseButton(1))
        {
            Vector3 newPosition = camera.ScreenToViewportPoint(Input.mousePosition);
            Vector3 direction = previousPosition - newPosition;
           
            float rotationAroundYAxis = transform.localRotation.x-direction.x * 180; // camera moves horizontally
            float rotationAroundXAxis = camera.transform.localRotation.y+direction.y * 180; // camera moves vertically

            //transform.position = target.position;

            //transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);

            //camera.transform.LookAt(transform, Vector3.up);
            //camera.transform.Translate(direction * rotationSpeed * Time.deltaTime);
            //transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);
            transform.Translate(new Vector3(-direction.x, 0, 0) * Time.deltaTime * rotationSpeed);
            transform.localRotation = Quaternion.Euler(0, -rotationAroundYAxis * 0.5f + transform.localRotation.y, 0);
            camera.transform.localRotation = Quaternion.Euler(rotationAroundXAxis * 0.5f + camera.transform.localRotation.x, 0, 0);
            transform.localRotation = Quaternion.Euler(0, rotationAroundYAxis * 0.5f + transform.localRotation.y, 0);
            //camera.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis * 0.5f, Space.World);


            //transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis, Space.World);


            //previousPosition = newPosition;
        }

    }
}

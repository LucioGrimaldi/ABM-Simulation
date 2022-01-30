using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GerardoUtils;

public class CameraTarget : MonoBehaviour {

    [SerializeField] private Camera cam;
    [SerializeField] private float distanceToTarget = 10;

    private Vector3 previousPosition;
    public bool follow;
    public Transform target;
    public float smoothSpeed = 0.150f, speed, zoom = 15;
    [SerializeField] Vector3 offset = new Vector3(10f, 5f, -5f);


    private void Update() {

    }


    private void LateUpdate()
    {
        if (follow)
        {

            if (cam.orthographic)
            {
                cam.fieldOfView += Input.GetAxis("Mouse ScrollWheel") * zoom;

            }
            else
            {
                cam.fieldOfView -= Input.GetAxis("Mouse ScrollWheel") * zoom;

                if (cam.fieldOfView < 9)
                    cam.fieldOfView = 9; 
                if (cam.fieldOfView > 120)
                    cam.fieldOfView = 120;
            }

            if (!Input.GetMouseButton(1))
            {
                Vector3 desiredPosition = target.position + offset;
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
                transform.position = smoothedPosition;

                transform.LookAt(target, Vector3.up);
            }

            if (Input.GetMouseButtonDown(1))
            {
                previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
            }
            else if (Input.GetMouseButton(1))
            {
                Vector3 newPosition = cam.ScreenToViewportPoint(Input.mousePosition);
                Vector3 direction = previousPosition - newPosition;

                float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
                float rotationAroundXAxis = direction.y * 180; // camera moves vertically

                //Vector3 smoothFollow = Vector3.Lerp(cam.transform.position, target.position, smoothSpeed);
                //cam.transform.position = smoothFollow;
                cam.transform.position = target.position;


                cam.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
                cam.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

                cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));

                previousPosition = newPosition;
            }
            if (Input.GetMouseButtonUp(1))
            {
                cam.fieldOfView = 60;
            }
        }

        //if (follow)
        //{
        //    //disabilita altri script camera movimento

        //    Vector3 desiredPosition = target.position + offset;
        //    Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        //    transform.position = smoothedPosition;

        //    //transform.Rotate(0, speed * Time.deltaTime, 0);

        //    transform.LookAt(target);
        //}
    }

    public void SetNewCameraTarget(Transform newTarget)
    {
        target = newTarget;
        follow = true;
    }
}
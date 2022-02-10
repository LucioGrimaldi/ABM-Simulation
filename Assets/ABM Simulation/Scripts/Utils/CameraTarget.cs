using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GerardoUtils;

public class CameraTarget : MonoBehaviour {

    [SerializeField] private Camera cam;
    [SerializeField] private static float distanceToTarget = 10;

    private Vector3 previousPosition, desiredPosition;
    public bool follow, zoomingIn, zoomingOut;
    public Transform target, tempTransform;
    public float smoothSpeed = 0.150f, speed, zoomMultiplier = 20, timeIn, timeOut, zoomDurationIn = 1f, zoomDurationOut = 1f;
    [SerializeField] Vector3 offset = new Vector3(10f, 5f, -5f);


    private void Update() {

    }


    private void LateUpdate()
    {
        if (follow)
        {
            if(target != null)
            {
                if (cam.orthographic)
                {
                    Vector3 dir = (target.position - transform.position).normalized;
                    transform.position = Vector3.Lerp(transform.position, transform.position + dir * (Input.GetAxis("Mouse ScrollWheel") * zoomMultiplier), Time.deltaTime * 15f);
                    offset = transform.position - target.position;
                    distanceToTarget = offset.magnitude;
                }
                else
                {
                    if (Input.GetAxis("Mouse ScrollWheel") != 0)
                    {
                        Vector3 dir = (target.position - transform.position).normalized;
                        Vector3 zoomIn = dir * (Input.GetAxis("Mouse ScrollWheel") * zoomMultiplier);
                        Vector3 newPosition = transform.position += zoomIn;
                        Vector3 newOffset = newPosition - target.position;
                        float newDistanceToTarget = newOffset.magnitude;
                        if (!(newDistanceToTarget < 3) && !(newDistanceToTarget > 200))
                        {
                            offset = newOffset;
                            distanceToTarget = newDistanceToTarget;
                            transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 15f);
                        }
                    }
                }
                if (!Input.GetMouseButton(1))
                {
                    tempTransform = Instantiate(new GameObject(), Vector3.zero, transform.rotation).transform;
                    tempTransform.position = target.position;
                    tempTransform.Rotate(new Vector3(1, 0, 0), 0f);
                    tempTransform.Rotate(new Vector3(0, 1, 0), 0f, Space.World);
                    tempTransform.Translate(new Vector3(0, 0, -distanceToTarget));

                    if (zoomingIn)
                    {
                        if(timeIn < zoomDurationIn)
                        {
                            transform.position = Vector3.Lerp(transform.position, tempTransform.position, timeIn / zoomDurationIn);
                            transform.rotation = Quaternion.Lerp(transform.rotation, tempTransform.rotation, timeIn / zoomDurationIn);
                            timeIn += Time.deltaTime;
                        }
                        else
                        {
                            zoomingIn = false;
                            timeIn = 0f;
                            transform.position = tempTransform.position;
                            transform.rotation = tempTransform.rotation;
                        }
                    }
                    else
                    {
                        transform.position = target.position;
                        transform.Rotate(new Vector3(1, 0, 0), 0f);
                        transform.Rotate(new Vector3(0, 1, 0), 0f, Space.World);
                        transform.Translate(new Vector3(0, 0, -distanceToTarget));
                    }                    
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

                    transform.position = target.position;
                    transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
                    transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);
                    transform.Translate(new Vector3(0, 0, -distanceToTarget));

                    offset = target.position - transform.position;
                    previousPosition = newPosition;
                }
                if (Input.GetMouseButtonUp(1))
                {
                    cam.fieldOfView = 60;
                }
            }
            else
            {
                follow = false;
            }
        }
        if (zoomingOut)
        {
            if (timeOut < zoomDurationOut)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(50, 75, -95), timeOut / zoomDurationOut);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(12.5f, 0, 0)), timeOut / zoomDurationOut);
                timeOut += Time.deltaTime;
            }
            else
            {
                zoomingOut = false;
                timeOut = 0f;
                transform.position = new Vector3(50, 75, -95);
                transform.rotation = Quaternion.Euler(new Vector3(12.5f, 0, 0));
            }
        }
    }

    public void SetNewCameraTarget(Transform newTarget)
    {
        target = newTarget;
        zoomingIn = true;
    }
}
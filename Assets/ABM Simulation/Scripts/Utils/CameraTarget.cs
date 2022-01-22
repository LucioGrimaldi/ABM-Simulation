using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GerardoUtils;

public class CameraTarget : MonoBehaviour {

    public enum Axis {
        XZ,
        XY,
    }

    [SerializeField] private Axis axis = Axis.XZ;
    [SerializeField] private float moveSpeed = 20f;
    
    public bool follow;
    public Transform target;
    public float smoothSpeed = 0.150f;
    [SerializeField] Vector3 offset = new Vector3(0f, 2f, -5f);


    private void Update() {
        float moveX = 0f;
        float moveY = 0f;


        //if (Input.GetKey(KeyCode.UpArrow))
        //{
        //    moveY = +1f;
        //}
        //if (Input.GetKey(KeyCode.DownArrow))
        //{
        //    moveY = -1f;
        //}
        //if (Input.GetKey(KeyCode.LeftArrow))
        //{
        //    moveX = -1f;
        //}
        //if (Input.GetKey(KeyCode.RightArrow))
        //{
        //    moveX = +1f;
        //}

        Vector3 moveDir;

        switch (axis) {
            default:
            case Axis.XZ:
                moveDir = new Vector3(moveX, 0, moveY).normalized;
                break;
            case Axis.XY:
                moveDir = new Vector3(moveX, moveY).normalized;
                break;
        }

        if (moveX != 0 || moveY != 0)
        {
            // Not idle
        }

        if (axis == Axis.XZ) {
            moveDir = UtilsClass.ApplyRotationToVectorXZ(moveDir, 0f);
        }

        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }


    private void LateUpdate()
    {
        if (follow)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            transform.LookAt(target);
        }
    }

    public void SetNewCameraTarget(Transform newTarget)
    {
        target = newTarget;
        follow = true;
    }
}

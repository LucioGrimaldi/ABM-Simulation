using UnityEngine;

namespace RuntimeSceneGizmo
{
	public class CameraMovement : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField] private float normalSpeed = 0.2f, fastSpeed = 0.5f, scrollSpeed = 8;
		[SerializeField] private Camera mainCamera;
		[SerializeField] private float movementTime = 10f;

#pragma warning restore 0649

		private Vector3 newPosition;
		private float speed;

		private void Awake()
		{
		}
        private void Start()
        {
			newPosition = transform.position;
        }
        private void Update()
		{
			
		}

        private void LateUpdate()
        {
			MoveCamera();
		}

		void MoveCamera()
		{
			newPosition = transform.position;
			if (Input.GetKey(KeyCode.LeftShift))
            {
				speed = fastSpeed;
            }
            else
            {
				speed = normalSpeed;
            }
			if (Input.GetKey(KeyCode.W))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += (transform.forward * speed);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}
			if (Input.GetKey(KeyCode.S))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += (transform.forward * -speed);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}
			if (Input.GetKey(KeyCode.Q))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += new Vector3(0, -speed, 0);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}
			if (Input.GetKey(KeyCode.E))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += new Vector3(0, speed, 0);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}
			if (Input.GetKey(KeyCode.A) || (Input.GetKey(KeyCode.LeftArrow)))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += (transform.right * -speed);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}
			if (Input.GetKey(KeyCode.D) || (Input.GetKey(KeyCode.RightArrow)))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += (transform.right * speed);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}

			//
			if (Input.GetKey(KeyCode.UpArrow))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += (transform.up * speed);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}
			if (Input.GetKey(KeyCode.DownArrow))
			{
				if (mainCamera.isActiveAndEnabled)
					newPosition += (transform.up * -speed);
				transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
			}

			//float scroll = Input.GetAxis("Mouse ScrollWheel");
			//mainCamera.transform.Translate(0, 0, scroll * scrollSpeed, Space.Self);

		}
	}
}
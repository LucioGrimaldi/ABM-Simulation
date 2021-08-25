using UnityEngine;

namespace RuntimeSceneGizmo
{
	public class CameraMovement : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField] private float sensitivity = 0.5f, speed = 20.0f;
		[SerializeField] private Camera mainCamera;

#pragma warning restore 0649

		private Vector3 prevMousePos;
		private Transform mainCamParent;

		private void Awake()
		{
			mainCamParent = Camera.main.transform.parent;
		}

		private void Update()
		{
			MoveCamera();

			if( Input.GetMouseButtonDown( 1 ) )
				prevMousePos = Input.mousePosition;
			else if( Input.GetMouseButton( 1 ) )
			{
				Vector3 mousePos = Input.mousePosition;
				Vector2 deltaPos = ( mousePos - prevMousePos ) * sensitivity;

				Vector3 rot = mainCamParent.localEulerAngles;
				while( rot.x > 180f )
					rot.x -= 360f;
				while( rot.x < -180f )
					rot.x += 360f;

				rot.x = Mathf.Clamp( rot.x - deltaPos.y, -89.8f, 89.8f );
				rot.y += deltaPos.x;
				rot.z = 0f;

				mainCamParent.localEulerAngles = rot;
				prevMousePos = mousePos;
			}
		}

		void MoveCamera()
		{
			if (Input.GetKey(KeyCode.W))
			{
				if (mainCamera.isActiveAndEnabled)
					mainCamera.transform.position = mainCamera.transform.position + Camera.main.transform.forward * speed * Time.deltaTime;
			}
			if (Input.GetKey(KeyCode.S))
			{
				if (mainCamera.isActiveAndEnabled)
					mainCamera.transform.position = mainCamera.transform.position - Camera.main.transform.forward * speed * Time.deltaTime;
			}
			if (Input.GetKey(KeyCode.Q))
			{
				if (mainCamera.isActiveAndEnabled)
					mainCamera.transform.position += new Vector3(0, -speed * Time.deltaTime, 0);
			}
			if (Input.GetKey(KeyCode.E))
			{
				if (mainCamera.isActiveAndEnabled)
					mainCamera.transform.position += new Vector3(0, speed * Time.deltaTime, 0);
			}
			if (Input.GetKey(KeyCode.A))
			{
				if (mainCamera.isActiveAndEnabled)
					mainCamera.transform.position = mainCamera.transform.position - Camera.main.transform.right * speed * Time.deltaTime;
			}
			if (Input.GetKey(KeyCode.D))
			{
				if (mainCamera.isActiveAndEnabled)
					mainCamera.transform.position = mainCamera.transform.position + Camera.main.transform.right * speed * Time.deltaTime;
			}
		}
	}
}
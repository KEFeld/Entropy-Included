using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    private Vector3 dragOrigin;

    void Update()
    {
        PanCamera();
        ZoomCameraCenteredOnMouse();
    }

    void PanCamera()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse button clicked
        {
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1)) // Right mouse button held down
        {
            Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Camera.main.transform.position += difference;
        }
    }

    void ZoomCameraCenteredOnMouse()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        float newZoom = Mathf.Clamp(Camera.main.orthographicSize - zoomDelta, minZoom, maxZoom);

        if (newZoom != Camera.main.orthographicSize)
        {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = Camera.main.transform.position - mouseWorldPosition; // Invert the direction calculation
            float zoomRatio = (newZoom - Camera.main.orthographicSize) / Camera.main.orthographicSize;
            Camera.main.transform.position += direction * zoomRatio;
            Camera.main.orthographicSize = newZoom;
        }
    }
}

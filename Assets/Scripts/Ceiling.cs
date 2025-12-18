using UnityEngine;

public class Ceiling : MonoBehaviour
{
    private BoxCollider2D boxCollider;
    private Camera cam;

    public float scrollSpeed = 100f;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        cam = Camera.main;
    }

    void Start()
    {
        ResetCeilingPosition();
    }

    void Update()
    {
        transform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        float leftEdge = transform.position.x - boxCollider.size.x / 2f;
        float camLeftEdge = cam.transform.position.x - (cam.orthographicSize * cam.aspect);
        if (leftEdge < camLeftEdge - 10f)
        {
            ResetCeilingPosition();
        }
    }

    private void ResetCeilingPosition()
    {
        float screenAspect = (float)Screen.width / Screen.height;
        float cameraHeight = cam.orthographicSize * 2f;
        float ceilingY = cam.transform.position.y + (cameraHeight / 2f) - 0.5f + 1f;
        float ceilingWidth = cameraHeight * screenAspect + 20f;

        transform.position = new Vector3(cam.transform.position.x + (ceilingWidth / 4.5f), ceilingY, 0f);
        boxCollider.size = new Vector2(ceilingWidth, 1f);
    }
}
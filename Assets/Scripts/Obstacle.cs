using UnityEngine;

public class Obstacle : MonoBehaviour
{
    Player player;

    // ДОБАВЛЕНО: Полярность шипа
    [Header("Polarity Settings")]
    public int obstaclePolarity = 0; // 0 = Neon, 1 = Dark
    [Header("Polarity Layers")]
    public int neonObstacleLayerIndex = 10; // НОВЫЙ слой для неоновых шипов
    public int darkObstacleLayerIndex = 11; // НОВЫЙ слой для темных шипов
    [Header("Polarity Colors")]
    public Color neonObstacleColor = new Color(0.2f, 1f, 1f, 1f);
    public Color darkObstacleColor = new Color(1f, 0.2f, 1f, 1f);

    private SpriteRenderer obstacleSpriteRenderer;

    private void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        obstacleSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Обновляем визуалы при старте
        UpdateObstacleVisuals();
    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (player == null || player.isDead)
            return;

        Vector2 pos = transform.position;

        pos.x -= player.velocity.x * Time.fixedDeltaTime;
        if (pos.x < -20)
        {
            Destroy(gameObject);
        }

        transform.position = pos;
    }

    // ДОБАВЬТЕ ЭТОТ МЕТОД для обработки столкновений
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null && !player.isDead)
            {
                player.hitObstacle(this);
            }
        }
    }

    // ДОБАВЛЕНО: Метод для установки полярности
    public void SetPolarity(int polarity)
    {
        obstaclePolarity = polarity;
        UpdateObstacleLayer();
        UpdateObstacleVisuals();
    }

    // ДОБАВЛЕНО: Обновление слоя шипа
    private void UpdateObstacleLayer()
    {
        gameObject.layer = (obstaclePolarity == 0 ? neonObstacleLayerIndex : darkObstacleLayerIndex);
    }

    // ДОБАВЛЕНО: Обновление визуалов
    private void UpdateObstacleVisuals()
    {
        if (obstacleSpriteRenderer != null)
        {
            obstacleSpriteRenderer.color = (obstaclePolarity == 0 ? neonObstacleColor : darkObstacleColor);
        }
    }
}
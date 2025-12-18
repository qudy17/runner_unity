using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    public int coinValue = 1;
    public float rotationSpeed = 100f;
    public bool shouldRotate = true;

    [Header("Collection Effect")]
    public float collectAnimationDuration = 0.2f;
    public float scaleUpAmount = 1.5f;

    private bool isCollected = false;
    private Player player;
    private SpriteRenderer spriteRenderer;
    private Collider2D coinCollider;

    void Awake()
    {
        // Настраиваем Rigidbody2D для работы триггеров
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // Устанавливаем слой Default чтобы не конфликтовать с полярностью
        gameObject.layer = 0;

        // Проверяем наличие коллайдера
        coinCollider = GetComponent<Collider2D>();
        if (coinCollider == null)
        {
            // Добавляем CircleCollider2D если нет
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.5f;
            coinCollider = circle;
        }

        // ВАЖНО: Убеждаемся что это триггер!
        coinCollider.isTrigger = true;
    }

    void Start()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        Debug.Log("Coin spawned at: " + transform.position + ", Collider: " + (coinCollider != null) + ", IsTrigger: " + (coinCollider != null && coinCollider.isTrigger));
    }

    void Update()
    {
        if (shouldRotate && !isCollected)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (player == null || player.isDead) return;

        Vector2 pos = transform.position;
        pos.x -= player.velocity.x * Time.fixedDeltaTime;
        transform.position = pos;

        if (pos.x < -30f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Coin OnTriggerEnter2D called! Other: " + other.gameObject.name);

        if (isCollected) return;

        Player playerComponent = other.GetComponent<Player>();
        if (playerComponent != null)
        {
            Debug.Log("Player detected! Collecting coin...");
            CollectCoin(playerComponent);
        }
    }

    // Альтернативный метод - проверка через OnTriggerStay2D
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isCollected) return;

        Player playerComponent = other.GetComponent<Player>();
        if (playerComponent != null)
        {
            CollectCoin(playerComponent);
        }
    }

    private void CollectCoin(Player playerComponent)
    {
        isCollected = true;
        playerComponent.AddCoins(coinValue);
        Debug.Log("💰 Coin collected! Value: " + coinValue);
        StartCoroutine(CollectAnimation());
    }

    private System.Collections.IEnumerator CollectAnimation()
    {
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < collectAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / collectAnimationDuration;

            float scale = 1f + (scaleUpAmount - 1f) * progress;
            transform.localScale = originalScale * scale;

            if (spriteRenderer != null)
            {
                Color newColor = originalColor;
                newColor.a = 1f - progress;
                spriteRenderer.color = newColor;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ground : MonoBehaviour
{
    Player player;

    public float groundHeight;
    public float groundRight;
    public float groundLeft;
    public float screenRight;
    BoxCollider2D collider;

    public bool didGenerateNext = false;
    public float generationThreshold = 30f;

    public Obstacle SpikePrefab;

    // Новые параметры для контроля разрывов
    public float minPlatformLength = 10f;
    public float maxPlatformLength = 15f;
    public float minPlatformHeight = 1f;
    public float maxPlatformHeight = 4f;
    public float minGapBetweenPlatforms = 2f;
    public float maxGapBetweenPlatforms = 5f;

    // Добавляем флаг для начальной платформы
    public bool isInitialPlatform = false;

    // Ссылка на child объект со спрайтом
    public Transform spriteChild;

    // Добавляем параметры для контроля высоты спавна
    public float maxHeightChange = 2f; // Максимальное изменение высоты между платформами
    public float minAllowedHeight = -3f; // Минимальная допустимая высота
    public float maxAllowedHeight = 8f; // Максимальная допустимая высота

    [Header("Polarity Settings")]
    public int platformPolarity = 0; // 0 = Neon, 1 = Dark
    [Header("Polarity Layers")]
    public int neonLayerIndex = 8;
    public int darkLayerIndex = 9;
    [Header("Polarity Colors")]
    public Color neonPlatformColor = new Color(0.2f, 1f, 1f, 1f); // Cyan-ish neon
    public Color darkPlatformColor = new Color(1f, 0.2f, 1f, 1f);   // Magenta-ish dark

    private void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        collider = GetComponent<BoxCollider2D>();

        float screenAspect = (float)Screen.width / (float)Screen.height;
        float cameraHeight = Camera.main.orthographicSize * 2;
        screenRight = Camera.main.transform.position.x + (cameraHeight * screenAspect / 2);

        // Находим child объект со спрайтом автоматически, если не назначен
        if (spriteChild == null && transform.childCount > 0)
        {
            spriteChild = transform.GetChild(0);
        }

        // Устанавливаем случайную длину ТОЛЬКО если это не начальная платформа
        if (!isInitialPlatform)
        {
            SetRandomPlatformLength();
        }

        // Подгоняем спрайт под размер коллайдера
        UpdateSpriteToMatchCollider();
        SetRandomPolarityIfNeeded();
        SetPlatformLayer();
        UpdatePlatformVisuals();
    }

    private void SetRandomPolarityIfNeeded()
    {
        if (!isInitialPlatform)
        {
            platformPolarity = Random.Range(0, 2);
        }
        // Для начальной платформы оставляем 0 (Neon) по умолчанию
    }

    private void SetPlatformLayer()
    {
        gameObject.layer = (platformPolarity == 0 ? neonLayerIndex : darkLayerIndex);
    }

    private void UpdatePlatformVisuals()
    {
        if (spriteChild != null)
        {
            SpriteRenderer sr = spriteChild.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = (platformPolarity == 0 ? neonPlatformColor : darkPlatformColor);
            }
        }
    }

    void Update()
    {
        groundHeight = transform.position.y + (collider.size.y / 2);
    }

    private void FixedUpdate()
    {
        if (player.isDead)
            return;

        Vector2 pos = transform.position;
        pos.x -= player.velocity.x * Time.fixedDeltaTime;

        groundRight = transform.position.x + (collider.size.x / 2);
        groundLeft = transform.position.x - (collider.size.x / 2);

        if (groundRight < -25f)
        {
            Destroy(gameObject);
            return;
        }

        if (!didGenerateNext && groundRight < screenRight + generationThreshold && groundRight > screenRight - 5f)
        {
            didGenerateNext = true;
            GenerateNextPlatform();
        }

        transform.position = pos;
    }

    void SetRandomPlatformLength()
    {
        // Меняем размер коллайдера
        Vector2 size = collider.size;
        size.x = Random.Range(minPlatformLength, maxPlatformLength);
        collider.size = size;

        // Обновляем спрайт чтобы соответствовал коллайдеру
        UpdateSpriteToMatchCollider();
    }

    void UpdateSpriteToMatchCollider()
    {
        // Подгоняем размер child-спрайта под размер коллайдера
        if (spriteChild != null)
        {
            // Устанавливаем scale спрайта равным размеру коллайдера
            Vector3 newScale = spriteChild.localScale;
            newScale.x = collider.size.x; // Спрайт будет такой же длины как коллайдер
            spriteChild.localScale = newScale;
        }
    }

    void GenerateNextPlatform()
    {
        GameObject newGround = Instantiate(gameObject);
        Ground newGroundScript = newGround.GetComponent<Ground>();

        // Убеждаемся что новая платформа НЕ считается начальной
        newGroundScript.isInitialPlatform = false;
        newGroundScript.SetRandomPolarityIfNeeded();
        newGroundScript.SetPlatformLayer();
        newGroundScript.UpdatePlatformVisuals();

        // Сбрасываем флаг генерации для новой платформы!
        newGroundScript.didGenerateNext = false;

        // Находим child объект для новой платформы
        if (newGroundScript.spriteChild == null && newGround.transform.childCount > 0)
        {
            newGroundScript.spriteChild = newGround.transform.GetChild(0);
        }

        // Устанавливаем случайную длину для новой платформы
        newGroundScript.SetRandomPlatformLength();

        BoxCollider2D newCollider = newGround.GetComponent<BoxCollider2D>();
        Vector2 newPos = CalculateNextPlatformPosition(newCollider.size.x);

        newGround.transform.position = newPos;

        // Удаляем GroundFall с новой платформы
        GroundFall existingFall = newGround.GetComponent<GroundFall>();
        if (existingFall != null)
        {
            Destroy(existingFall);
        }

        // Случайно добавляем падающую платформу
        if (Random.Range(0, 5) == 0)
        {
            GroundFall newFall = newGround.AddComponent<GroundFall>();
            newFall.fallSpeed = Random.Range(1.0f, 3.0f);
        }

        GenerateObstacles(newGround, newGroundScript, newCollider);
    }

    Vector2 CalculateNextPlatformPosition(float nextPlatformWidth)
    {
        Vector2 newPos = Vector2.zero;

        float gap = Random.Range(minGapBetweenPlatforms, maxGapBetweenPlatforms);

        // Если groundRight уже прошел экран, создаем платформу справа от экрана
        float spawnX = Mathf.Max(groundRight, screenRight) + gap + (nextPlatformWidth / 2);
        newPos.x = spawnX;

        // Генерируем случайное изменение высоты в пределах maxHeightChange
        float heightDifference = Random.Range(-maxHeightChange, maxHeightChange);

        // Вычисляем новую высоту на основе текущей высоты + изменение
        newPos.y = transform.position.y + heightDifference;

        // Ограничиваем высоту разумными пределами
        newPos.y = Mathf.Clamp(newPos.y, minAllowedHeight, maxAllowedHeight);

        return newPos;
    }

    void GenerateObstacles(GameObject groundObject, Ground groundScript, BoxCollider2D groundCollider)
    {
        int obstacleCount = Random.Range(0, 3);

        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject obstacle = Instantiate(SpikePrefab.gameObject);

            // Получаем коллайдер шипа для правильного позиционирования
            Collider2D obstacleCollider = obstacle.GetComponent<Collider2D>();
            float obstacleHeight = obstacleCollider != null ? obstacleCollider.bounds.size.y : 0f;

            // Рассчитываем позицию шипа
            float platformLeft = groundObject.transform.position.x - (groundCollider.size.x / 2);
            float platformRight = groundObject.transform.position.x + (groundCollider.size.x / 2);
            float obstacleX = Random.Range(platformLeft + 1f, platformRight - 1f); // Отступ от краев

            // Позиционируем шип НА платформе, а не на groundHeight
            float platformTop = groundObject.transform.position.y + (groundCollider.size.y / 2);
            float obstacleY = platformTop + (obstacleHeight / 2); // Ставим шип поверх платформы

            float manualOffset = -0.2f;
            obstacleY +=  manualOffset;

            Vector2 obstaclePos = new Vector2(obstacleX, obstacleY);
            obstacle.transform.position = obstaclePos;
            obstacle.layer = groundObject.layer;


            GroundFall fall = groundObject.GetComponent<GroundFall>();
            if (fall != null)
            {
                Obstacle obst = obstacle.GetComponent<Obstacle>();
                if (obst != null)
                {
                    fall.obstacles.Add(obst);
                }
            }
        }
    }
}
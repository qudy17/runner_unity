using UnityEngine;
using System.Collections.Generic;

public class DynamicStarBackground : MonoBehaviour
{
    [Header("Star Settings")]
    public int maxStars = 80;
    public float spawnAreaWidth = 25f;
    public float spawnAreaHeight = 15f;

    [Header("Star Colors")]
    public Color neonStarColor = new Color(0.2f, 1f, 1f, 1f); // Cyan-ish neon
    public Color darkStarColor = new Color(1f, 0.2f, 1f, 1f);   // Magenta-ish dark

    [Header("Star Shape Settings")]
    public int minPoints = 4;     // Минимальное количество лучей
    public int maxPoints = 8;     // Максимальное количество лучей
    public float minInnerRadius = 0.3f; // Относительный размер центра звезды
    public float maxInnerRadius = 0.7f;

    [Header("Spawn Animation")]
    public float spawnGrowDuration = 1.5f;    // Время появления звезды
    public AnimationCurve spawnGrowCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float spawnFadeInDuration = 0.8f;  // Время появления прозрачности

    [Header("Animation Settings")]
    public float minPulseSpeed = 0.5f;
    public float maxPulseSpeed = 2f;
    public float minPulseScale = 0.8f;
    public float maxPulseScale = 1.2f;
    public float minRotationSpeed = -10f;
    public float maxRotationSpeed = 10f;

    [Header("Movement Settings")]
    public float minMoveSpeed = 0.05f;
    public float maxMoveSpeed = 0.2f;
    public Vector2 moveDirection = new Vector2(-0.3f, 0.15f);

    [Header("Twinkle Settings")]
    public float minTwinkleSpeed = 0.3f;
    public float maxTwinkleSpeed = 1.5f;

    [Header("Lifecycle Settings")]
    public float minLifetime = 4f;
    public float maxLifetime = 10f;
    public float respawnDelay = 0.05f;

    private List<StarData> activeStars = new List<StarData>();
    private Material starMaterial;
    private Camera mainCamera;

    private class StarData
    {
        public GameObject starObject;
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public int points;           // Количество лучей
        public float innerRadius;    // Относительный размер центра
        public float size;           // Размер звезды
        public bool isNeon;
        public Color baseColor;

        // Анимационные параметры
        public float pulseSpeed;
        public float pulseTimer;
        public float rotationSpeed;
        public float twinkleSpeed;
        public float twinkleTimer;
        public float lifetime;
        public float currentLife;

        // Движение
        public float moveSpeed;
        public Vector3 position;

        // Состояние анимации
        public bool isSpawning = true;
        public float spawnTimer = 0f;
    }

    void Start()
    {
        mainCamera = Camera.main;

        // Создаем материал для звезд
        CreateStarMaterial();

        // Создаем звезды
        for (int i = 0; i < maxStars; i++)
        {
            CreateStar();
        }

        // Запускаем корутину для обновления
        StartCoroutine(UpdateStars());
    }

    void CreateStarMaterial()
    {
        // Создаем простой unlit материал с альфа-блендингом
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        starMaterial = new Material(shader);
    }

    void CreateStar()
    {
        GameObject star = new GameObject("Star");
        MeshFilter meshFilter = star.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = star.AddComponent<MeshRenderer>();

        // Случайные параметры звезды
        bool isNeon = Random.value > 0.5f;
        int points = Random.Range(minPoints, maxPoints + 1);
        float innerRadius = Random.Range(minInnerRadius, maxInnerRadius);
        float size = Random.Range(0.1f, 0.2f);

        // Цвет звезды
        Color baseColor = isNeon ? neonStarColor : darkStarColor;
        baseColor.a = 0f; // Начинаем с полностью прозрачной

        // Создаем меш звезды
        Mesh starMesh = CreateStarMesh(points, innerRadius);
        meshFilter.mesh = starMesh;
        meshRenderer.material = new Material(starMaterial); // Копия материала для каждой звезды
        meshRenderer.material.color = baseColor;

        // Позиция
        Vector3 position = GetRandomSpawnPosition();
        star.transform.position = position;
        star.transform.localScale = Vector3.zero; // Начинаем с нулевого размера

        // Создаем данные звезды
        StarData data = new StarData
        {
            starObject = star,
            meshRenderer = meshRenderer,
            meshFilter = meshFilter,
            points = points,
            innerRadius = innerRadius,
            size = size,
            isNeon = isNeon,
            baseColor = new Color(baseColor.r, baseColor.g, baseColor.b, Random.Range(0.4f, 0.9f)), // Целевая прозрачность

            // Анимационные параметры
            pulseSpeed = Random.Range(minPulseSpeed, maxPulseSpeed),
            pulseTimer = Random.Range(0f, Mathf.PI * 2),
            rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed),
            twinkleSpeed = Random.Range(minTwinkleSpeed, maxTwinkleSpeed),
            twinkleTimer = Random.Range(0f, Mathf.PI * 2),
            lifetime = Random.Range(minLifetime, maxLifetime),
            currentLife = 0f,

            // Движение
            moveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed),
            position = position,

            // Анимация появления
            isSpawning = true,
            spawnTimer = 0f
        };

        activeStars.Add(data);

        // Запускаем анимацию появления
        StartCoroutine(SpawnStarAnimation(data));
    }

    Mesh CreateStarMesh(int points, float innerRadius)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        float angleStep = 360f / (points * 2);

        // Центральная вершина
        vertices.Add(Vector3.zero);
        uv.Add(new Vector2(0.5f, 0.5f));

        // Создаем вершины для звезды
        for (int i = 0; i < points * 2; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float radius = (i % 2 == 0) ? 1f : innerRadius;

            Vector3 vertex = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );

            vertices.Add(vertex);

            // UV координаты
            Vector2 uvCoord = new Vector2(
                (vertex.x + 1) * 0.5f,
                (vertex.y + 1) * 0.5f
            );
            uv.Add(uvCoord);
        }

        // Создаем треугольники
        for (int i = 1; i <= points * 2; i++)
        {
            int next = (i % (points * 2)) + 1;

            triangles.Add(0);      // Центр
            triangles.Add(i);      // Текущая вершина
            triangles.Add(next);   // Следующая вершина
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    Vector3 GetRandomSpawnPosition()
    {
        float spawnX, spawnY;

        // 30% шанс спавна по краям, 70% - в случайной позиции внутри области
        if (Random.value < 0.3f)
        {
            // Спавн по краям
            if (Random.value > 0.5f)
            {
                spawnX = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
                spawnY = Random.value > 0.5f ? spawnAreaHeight / 2 : -spawnAreaHeight / 2;
            }
            else
            {
                spawnX = Random.value > 0.5f ? spawnAreaWidth / 2 : -spawnAreaWidth / 2;
                spawnY = Random.Range(-spawnAreaHeight / 2, spawnAreaHeight / 2);
            }
        }
        else
        {
            // Спавн в случайной позиции внутри области
            spawnX = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
            spawnY = Random.Range(-spawnAreaHeight / 2, spawnAreaHeight / 2);
        }

        // Звезды должны быть впереди камеры
        // Если камера в Z = -10, то звезды могут быть в Z = -9 до 0
        float spawnZ = Random.Range(-9f, 0f);

        return new Vector3(spawnX, spawnY, spawnZ);
    }

    System.Collections.IEnumerator SpawnStarAnimation(StarData star)
    {
        float growTimer = 0f;
        float fadeTimer = 0f;

        // Начальный размер - почти точка
        float startSize = star.size * 0.05f;
        star.starObject.transform.localScale = Vector3.one * startSize;

        // Начальный цвет - почти прозрачный
        Color startColor = star.baseColor;
        startColor.a = 0f;
        star.meshRenderer.material.color = startColor;

        // Анимация роста
        while (growTimer < spawnGrowDuration)
        {
            if (star.starObject == null) yield break;

            growTimer += Time.deltaTime;
            float growT = growTimer / spawnGrowDuration;
            float curveValue = spawnGrowCurve.Evaluate(growT);

            // Интерполируем размер
            float currentSize = Mathf.Lerp(startSize, star.size, curveValue);
            star.starObject.transform.localScale = Vector3.one * currentSize;

            // Интерполируем прозрачность (немного отстает от роста)
            float fadeT = Mathf.Clamp01(growTimer / spawnFadeInDuration);
            Color currentColor = star.baseColor;
            currentColor.a = Mathf.Lerp(0f, star.baseColor.a, fadeT);
            star.meshRenderer.material.color = currentColor;

            // Легкое вращение во время появления
            star.starObject.transform.Rotate(0, 0, 10f * Time.deltaTime * curveValue);

            yield return null;
        }

        // Завершаем анимацию появления
        star.isSpawning = false;
        star.starObject.transform.localScale = Vector3.one * star.size;
        star.meshRenderer.material.color = star.baseColor;
    }

    System.Collections.IEnumerator UpdateStars()
    {
        while (true)
        {
            for (int i = activeStars.Count - 1; i >= 0; i--)
            {
                StarData star = activeStars[i];

                // Пропускаем звезды в процессе появления
                if (star.isSpawning)
                    continue;

                // Обновляем время жизни
                star.currentLife += Time.deltaTime;

                // Пульсация (размер) - только после появления
                star.pulseTimer += Time.deltaTime * star.pulseSpeed;
                float pulseFactor = (Mathf.Sin(star.pulseTimer) + 1f) / 2f;
                float pulseScale = Mathf.Lerp(minPulseScale, maxPulseScale, pulseFactor);

                // Мерцание (прозрачность)
                star.twinkleTimer += Time.deltaTime * star.twinkleSpeed;
                float twinkleFactor = (Mathf.Sin(star.twinkleTimer) + 1f) / 2f;
                float alpha = Mathf.Lerp(0.3f, 1f, twinkleFactor);

                // Применяем анимации
                star.starObject.transform.localScale = Vector3.one * star.size * pulseScale;
                star.starObject.transform.Rotate(0, 0, star.rotationSpeed * Time.deltaTime);

                // Обновляем цвет с мерцанием
                Color currentColor = star.baseColor;
                currentColor.a = alpha;
                star.meshRenderer.material.color = currentColor;

                // Движение - только после полного появления
                Vector3 moveOffset = moveDirection.normalized * star.moveSpeed * Time.deltaTime;
                star.position += moveOffset;
                star.starObject.transform.position = star.position;

                // Периодически меняем форму (дополнительная анимация)
                if (Random.value < 0.001f) // 0.1% шанс каждый кадр
                {
                    AnimateShapeChange(star);
                }

                // Проверка на выход за границы или окончание времени жизни
                if (IsOutOfBounds(star.position) || star.currentLife >= star.lifetime)
                {
                    StartCoroutine(FadeOutStar(star));
                    activeStars.RemoveAt(i);

                    yield return new WaitForSeconds(respawnDelay);
                    CreateStar();
                }
            }

            yield return null;
        }
    }

    void AnimateShapeChange(StarData star)
    {
        // Плавно меняем внутренний радиус для эффекта "схлопывания/расширения"
        float targetInnerRadius = Random.Range(minInnerRadius, maxInnerRadius);
        StartCoroutine(AnimateInnerRadius(star, targetInnerRadius, 1f));
    }

    System.Collections.IEnumerator AnimateInnerRadius(StarData star, float targetRadius, float duration)
    {
        float startRadius = star.innerRadius;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // ПРОВЕРКА: если звезда была уничтожена, прерываем корутину
            if (star.starObject == null || star.meshFilter == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Плавная интерполяция
            t = Mathf.SmoothStep(0, 1, t);
            star.innerRadius = Mathf.Lerp(startRadius, targetRadius, t);

            // Обновляем меш
            UpdateStarMesh(star);

            yield return null;
        }
    }

    void UpdateStarMesh(StarData star)
    {
        if (star == null || star.meshFilter == null || star.starObject == null)
            return;

        Mesh newMesh = CreateStarMesh(star.points, star.innerRadius);
        star.meshFilter.mesh = newMesh;
    }

    bool IsOutOfBounds(Vector3 position)
    {
        float buffer = 3f;
        return Mathf.Abs(position.x) > spawnAreaWidth / 2 + buffer ||
               Mathf.Abs(position.y) > spawnAreaHeight / 2 + buffer;
    }

    System.Collections.IEnumerator FadeOutStar(StarData star)
    {
        float fadeDuration = 0.8f;
        float elapsed = 0f;
        Color startColor = star.meshRenderer.material.color;

        while (elapsed < fadeDuration)
        {
            if (star.starObject == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            Color fadedColor = startColor;
            fadedColor.a = Mathf.Lerp(startColor.a, 0f, t);
            star.meshRenderer.material.color = fadedColor;

            // Также уменьшаем размер
            float scale = Mathf.Lerp(1f, 0.1f, t);
            star.starObject.transform.localScale = Vector3.one * star.size * scale;

            yield return null;
        }

        if (star.starObject != null)
            Destroy(star.starObject);
    }

    void OnDestroy()
    {
        if (starMaterial != null)
            Destroy(starMaterial);

        foreach (StarData star in activeStars)
        {
            if (star.starObject != null)
                Destroy(star.starObject);
        }
        activeStars.Clear();
    }

    // Оптимизация: создание пула мешей
    private Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();

    Mesh GetCachedStarMesh(int points, float innerRadius)
    {
        string key = $"{points}_{innerRadius:F2}";

        if (!meshCache.ContainsKey(key))
        {
            meshCache[key] = CreateStarMesh(points, innerRadius);
        }

        return meshCache[key];
    }

    void UpdateStarMeshCached(StarData star)
    {
        if (star == null || star.meshFilter == null || star.starObject == null)
            return;

        Mesh cachedMesh = GetCachedStarMesh(star.points, star.innerRadius);
        star.meshFilter.mesh = cachedMesh;
    }
}
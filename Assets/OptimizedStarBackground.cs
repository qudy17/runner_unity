using UnityEngine;
using System.Collections.Generic;

public class OptimizedStarBackground : MonoBehaviour
{
    [Header("Star Settings")]
    public int maxStars = 100;
    public float minStarSize = 0.03f;
    public float maxStarSize = 0.15f;

    [Header("Star Colors")]
    public Color neonStarColor = new Color(0.2f, 1f, 1f, 1f);
    public Color darkStarColor = new Color(1f, 0.2f, 1f, 1f);

    [Header("Performance")]
    [Range(1, 10)] public int updateRate = 2; // Обновлять каждые N кадров
    public bool useFixedUpdate = true;

    // Пулы объектов (NO GC!)
    private Star[] stars;
    private Matrix4x4[] matrices;
    private Vector4[] colors;
    private Mesh starMesh;
    private Material starMaterial;
    private MaterialPropertyBlock propertyBlock;
    private int activeStars = 0;

    private struct Star
    {
        public Vector3 position;
        public Vector3 velocity;
        public float size;
        public Color color;
        public float pulseSpeed;
        public float pulsePhase;
        public float twinkleSpeed;
        public float twinklePhase;
        public float lifetime;
        public float age;
    }

    void Start()
    {
        // Предварительное выделение памяти (NO GC во время работы)
        InitializePools();
        InitializeStars();

        // Создаем один меш для всех звезд
        starMesh = CreateSimpleStarMesh();

        // Создаем материал
        CreateStarMaterial();

        // PropertyBlock для изменения цвета без создания новых материалов
        propertyBlock = new MaterialPropertyBlock();
    }

    void InitializePools()
    {
        // Создаем массивы один раз
        stars = new Star[maxStars];
        matrices = new Matrix4x4[maxStars];
        colors = new Vector4[maxStars];
    }

    void InitializeStars()
    {
        for (int i = 0; i < maxStars; i++)
        {
            stars[i] = CreateRandomStar(true);
        }
        activeStars = maxStars;
    }

    Star CreateRandomStar(bool initialSpawn = false)
    {
        Star star = new Star();

        star.position = GetRandomSpawnPosition();
        star.velocity = new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(-0.02f, 0.02f),
            0
        );

        star.size = Random.Range(minStarSize, maxStarSize);

        // Случайный цвет
        star.color = (Random.value > 0.5f ? neonStarColor : darkStarColor);

        // Начальная прозрачность
        if (initialSpawn)
            star.color.a = Random.Range(0.3f, 0.9f);
        else
            star.color.a = 0f; // Для плавного появления

        // Анимационные параметры
        star.pulseSpeed = Random.Range(0.3f, 1.0f);
        star.pulsePhase = Random.Range(0f, Mathf.PI * 2);
        star.twinkleSpeed = Random.Range(0.5f, 2.0f);
        star.twinklePhase = Random.Range(0f, Mathf.PI * 2);

        star.lifetime = Random.Range(6f, 12f);
        star.age = initialSpawn ? Random.Range(0f, star.lifetime) : 0f;

        return star;
    }

    Vector3 GetRandomSpawnPosition()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return Vector3.zero;

        float orthoSize = cam.orthographicSize;
        float aspect = cam.aspect;

        // Спавн за пределами экрана для плавного появления
        float width = orthoSize * aspect * 1.8f;
        float height = orthoSize * 1.8f;

        Vector3 camPos = cam.transform.position;

        return new Vector3(
            Random.Range(-width, width) + camPos.x,
            Random.Range(-height, height) + camPos.y,
            camPos.z + 5f // Перед камерой
        );
    }

    Mesh CreateSimpleStarMesh()
    {
        // Простая 4-лучевая звезда
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[9];
        int[] triangles = new int[24];

        // Центр
        vertices[0] = Vector3.zero;

        // 4 внешних луча
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        }

        // 4 внутренних точки (для формы звезды)
        for (int i = 0; i < 4; i++)
        {
            float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
            vertices[i + 5] = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0);
        }

        // Треугольники для формы звезды
        int triIndex = 0;
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;

            // Треугольники между лучами
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 1;
            triangles[triIndex++] = i + 5;

            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 5;
            triangles[triIndex++] = next + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Помечаем как не собирать мусор
        mesh.hideFlags = HideFlags.DontSave;

        return mesh;
    }

    void CreateStarMaterial()
    {
        // Используем самый простой шейдер
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("UI/Default");

        starMaterial = new Material(shader);
        starMaterial.enableInstancing = true;
    }

    private int frameCounter = 0;

    void Update()
    {
        if (!useFixedUpdate)
        {
            frameCounter++;
            if (frameCounter % updateRate == 0)
            {
                UpdateStars(Time.deltaTime * updateRate);
                frameCounter = 0;
            }

            RenderStars();
        }
    }

    void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            UpdateStars(Time.fixedDeltaTime);
        }
    }

    void LateUpdate()
    {
        if (useFixedUpdate)
        {
            RenderStars();
        }
    }

    void UpdateStars(float deltaTime)
    {
        for (int i = 0; i < activeStars; i++)
        {
            Star star = stars[i];

            // Обновляем возраст
            star.age += deltaTime;

            // Плавное появление
            if (star.age < 1.5f)
            {
                star.color.a = Mathf.Lerp(0f, 0.7f, star.age / 1.5f);
            }

            // Мерцание
            float twinkle = (Mathf.Sin(star.age * star.twinkleSpeed + star.twinklePhase) + 1f) * 0.5f;
            float currentAlpha = Mathf.Lerp(0.4f, 0.9f, twinkle) * star.color.a;

            // Пульсация размера (очень легкая)
            float pulse = (Mathf.Sin(star.age * star.pulseSpeed + star.pulsePhase) + 1f) * 0.5f;
            float currentSize = star.size * (0.9f + pulse * 0.2f);

            // Движение
            star.position += star.velocity * deltaTime;

            // Проверка на респавн
            if (star.age > star.lifetime || IsOutOfBounds(star.position))
            {
                star = CreateRandomStar();
            }
            else
            {
                // Обновляем прозрачность
                Color updatedColor = star.color;
                updatedColor.a = currentAlpha;
                star.color = updatedColor;

                // Сохраняем в массивы для отрисовки
                matrices[i] = Matrix4x4.TRS(star.position, Quaternion.identity, Vector3.one * currentSize);
                colors[i] = star.color;
            }

            stars[i] = star;
        }
    }

    bool IsOutOfBounds(Vector3 position)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 viewportPos = cam.WorldToViewportPoint(position);
        return viewportPos.x < -0.3f || viewportPos.x > 1.3f ||
               viewportPos.y < -0.3f || viewportPos.y > 1.3f;
    }

    void RenderStars()
    {
        if (starMaterial == null || starMesh == null || activeStars == 0)
            return;

        // Используем PropertyBlock для изменения цветов
        propertyBlock.SetVectorArray("_Color", colors);

        // Отрисовываем все звезды за один вызов с GPU Instancing
        Graphics.DrawMeshInstanced(
            starMesh,
            0,
            starMaterial,
            matrices,
            activeStars,
            propertyBlock,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            false,
            0, // layer
            null,
            UnityEngine.Rendering.LightProbeUsage.Off
        );
    }

    void OnDestroy()
    {
        // Очистка
        if (starMaterial != null)
        {
            Destroy(starMaterial);
            starMaterial = null;
        }

        if (starMesh != null)
        {
            Destroy(starMesh);
            starMesh = null;
        }
    }

    // Метод для изменения настроек во время работы
    public void SetStarCount(int count)
    {
        if (count <= 0 || count > maxStars)
            return;

        activeStars = count;
    }

    public int GetActiveStarCount()
    {
        return activeStars;
    }
}
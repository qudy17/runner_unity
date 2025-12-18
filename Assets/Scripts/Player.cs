using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float gravity;                   // Сила гравитации (отрицательное значение)
    public Vector2 velocity;                // Текущая скорость игрока (x - горизонтальная, y - вертикальная)
    public float maxXVelocity = 100;        // Максимальная горизонтальная скорость
    public float maxAcceleration = 10;      // Максимальное ускорение
    public float acceleration = 10;         // Текущее ускорение (увеличивает velocity.x)
    public float distance = 0;              // Пройденная дистанция (счетчик очков)
    public float jumpVelocity = 20;         // Сила прыжка (начальная вертикальная скорость)
    public float groundHeight = 10;         // Текущая высота земли под игроком
    public bool isGrounded = false;         // Стоит ли игрок на земле

    public bool isHoldingJump = false;      // Зажата ли кнопка прыжка
    public float maxHoldJumpTime = 0.4f;    // Максимальное время удержания прыжка
    public float maxMaxHoldJumpTime = 0.4f; // Базовое максимальное время удержания
    public float holdJumpTimer = 0.0f;      // Таймер удержания прыжка

    public float jumpGroundThreshold = 1;   // Дистанция от земли, при которой еще можно прыгнуть

    public bool isDead = false;             // Умер ли игрок
                                            // === СИСТЕМА МОНЕТ ===
                                            // === СИСТЕМА МОНЕТ (обновлённая) ===
    [Header("Coin System")]
    public int sessionCoins = 0;            // Монеты собранные в текущей сессии

   

    // totalCoins теперь загружается/сохраняется автоматически
    private int _totalCoins = 0;
    public int totalCoins
    {
        get { return _totalCoins; }
        set
        {
            _totalCoins = value;
            SaveCoins(); // Автосохранение при изменении
        }
    }

    // Ключ для PlayerPrefs
    private const string COINS_SAVE_KEY = "PlayerTotalCoins";

    // ДОБАВЛЕНО: Переменные для двойного прыжка
    public int maxJumpCount = 2;            // Максимальное количество прыжков
    private int currentJumpCount = 0;       // Текущее количество использованных прыжков
    public bool canDoubleJump = true;       // Можно ли делать двойной прыжок
    //private bool wasGrounded = false;       // Был ли игрок на земле в предыдущем кадре

    [Header("Polarity Settings")]
    public int currentPolarity = 0; // 0 = Neon, 1 = Dark
    [Header("Polarity Layers")]
    public int neonGroundLayerIndex = 8;
    public int darkGroundLayerIndex = 9;
    [Header("Polarity Colors")]
    public Color neonPlayerColor = new Color(0.2f, 1f, 1f, 1f);
    public Color darkPlayerColor = new Color(1f, 0.2f, 1f, 1f);

    private LayerMask neonMask;
    private LayerMask darkMask;
    private int playerLayer;
    private SpriteRenderer playerSpriteRenderer;

    GroundFall fall;
    CameraController cameraController;

    private PlayerInput playerInput;
    private InputAction jumpAction;
    private bool jumpPressed = false;
    private bool jumpReleased = false;

    private BoxCollider2D playerCollider;
    public float groundCheckDistance = 0.1f;

    // === МЕХАНИКА РЫВКА (DASH/AIR DASH) ===
    public float dashBoostSpeed = 250f;     // Скорость рывка (200-300)
    public float dashDuration = 0.15f;      // Длительность (0.1-0.2 сек)
    public float dashCooldown = 0.8f;       // Кулдаун (0.5-1 сек)
    public float airDeceleration = 5f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private bool dashPressed = false;

    private InputAction dashAction;
    private float preDashVelocityX = 0f;  // << НОВАЯ: Сохраняет скорость ДО dash

    private InputAction switchPolarityAction;

    // === НОВОЕ: Для потолка ===
    [Header("Ceiling Settings")]
    public LayerMask ceilingMask; // Назначьте слой "Ceiling" в инспекторе
    public float ceilingCheckDistance = 0.2f; // Буфер для raycast вверх

    void Start()
    {
        // Загружаем сохранённые монеты при старте
        LoadCoins();

        Debug.Log("Player start position: " + transform.position);
        Debug.Log("💰 Loaded total coins: " + _totalCoins);

        cameraController = Camera.main.GetComponent<CameraController>();
        playerCollider = GetComponent<BoxCollider2D>();

        // === ВАЖНО: Проверяем землю при старте ===
        Vector2 pos = transform.position;
        Vector2 groundRayOrigin = new Vector2(pos.x, pos.y - (playerCollider.bounds.size.y / 2));

        // Используем Default + Neon маску для начальной проверки
        int startMask = (1 << neonGroundLayerIndex) | (1 << darkGroundLayerIndex);
        RaycastHit2D groundHit = Physics2D.Raycast(groundRayOrigin, Vector2.down, 1f, startMask);

        if (groundHit.collider != null)
        {
            Ground ground = groundHit.collider.GetComponent<Ground>();
            if (ground != null)
            {
                groundHeight = ground.groundHeight;
                // Ставим игрока точно на платформу
                pos.y = groundHeight + (playerCollider.bounds.size.y / 2);
                transform.position = pos;
                isGrounded = true;
                Debug.Log("✅ Player placed on ground at height: " + groundHeight);
            }
        }
        else
        {
            isGrounded = false;
            Debug.Log("⚠️ No ground found under player at start!");
        }

        // ... остальной код Start() ...

        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }

        jumpAction = playerInput.actions["Jump"];
        if (jumpAction != null)
        {
            jumpAction.started += OnJumpStarted;
            jumpAction.canceled += OnJumpCanceled;
        }

        dashAction = playerInput.actions["Dash"];
        if (dashAction != null)
        {
            dashAction.started += OnDashStarted;
        }

        neonMask = 1 << neonGroundLayerIndex;
        darkMask = 1 << darkGroundLayerIndex;
        playerLayer = gameObject.layer;

        switchPolarityAction = playerInput.actions["SwitchPolarity"];
        if (switchPolarityAction != null)
        {
            switchPolarityAction.started += OnPolaritySwitch;
        }

        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.color = neonPlayerColor;
        }

        Physics2D.IgnoreLayerCollision(playerLayer, 10, currentPolarity != 0);
        Physics2D.IgnoreLayerCollision(playerLayer, 11, currentPolarity != 1);
    }

    void OnDestroy()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpStarted;
            jumpAction.canceled -= OnJumpCanceled;
        }

        if (dashAction != null)
        {
            dashAction.started -= OnDashStarted;
        }

        if (switchPolarityAction != null)
        {
            switchPolarityAction.started -= OnPolaritySwitch;
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        jumpReleased = true;
    }

    private void OnDashStarted(InputAction.CallbackContext context)
    {
        dashPressed = true;
        Debug.Log("Dash pressed! Cooldown ready: " + (dashCooldownTimer <= 0f)); // ТЕСТ
    }

    private void OnPolaritySwitch(InputAction.CallbackContext context)
    {
        SwitchPolarity();
    }

    void Update()
    {
        // ТОЛЬКО сбор input'а через события Input System
        // Вся игровая логика перенесена в FixedUpdate
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        // === РЫВОК: КУЛДАУН ===
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.fixedDeltaTime);

        // === ЗАПУСК РЫВКА ===
        if (dashPressed && dashCooldownTimer <= 0f && !isDashing)
        {
            dashPressed = false;
            preDashVelocityX = velocity.x;
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

        // === ЛОГИКА РЫВКА ===
        if (isDashing)
        {
            velocity.x = dashBoostSpeed;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                velocity.x = preDashVelocityX;
            }
        }
        else
        {
            velocity.x = Mathf.Min(velocity.x, maxXVelocity);
        }

        Vector2 pos = transform.position;

        // Сохраняем предыдущее состояние
        bool wasGroundedLastFrame = isGrounded;

        // === ПРИМЕНЯЕМ ГРАВИТАЦИЮ И ВЕРТИКАЛЬНОЕ ДВИЖЕНИЕ (если в воздухе) ===
        if (!isGrounded)
        {
            // Удержание прыжка
            if (isHoldingJump)
            {
                holdJumpTimer += Time.fixedDeltaTime;
                if (holdJumpTimer >= maxHoldJumpTime)
                {
                    isHoldingJump = false;
                }
            }

            // Применяем гравитацию (только если не удерживаем прыжок)
            if (!isHoldingJump)
            {
                velocity.y += gravity * Time.fixedDeltaTime;
            }

            // Проверка потолка (когда летим вверх)
            if (velocity.y > 0)
            {
                float ceilingRayLength = Mathf.Abs(velocity.y * Time.fixedDeltaTime) + ceilingCheckDistance;
                Vector2 ceilingRayOrigin = new Vector2(pos.x, pos.y + (playerCollider.bounds.size.y / 2));
                RaycastHit2D ceilingHit = Physics2D.Raycast(ceilingRayOrigin, Vector2.up, ceilingRayLength, ceilingMask);

                if (ceilingHit.collider != null)
                {
                    pos.y = ceilingHit.point.y - (playerCollider.bounds.size.y / 2) - 0.01f;
                    velocity.y = 0;
                    isHoldingJump = false;
                }
            }

            // Применяем вертикальное движение
            pos.y += velocity.y * Time.fixedDeltaTime;
        }

        // === ПРОВЕРКА ЗЕМЛИ (ВСЕГДА выполняется) ===
        float groundRayLength = groundCheckDistance;

        // Удлиняем луч если падаем
        if (velocity.y < 0)
        {
            groundRayLength = Mathf.Abs(velocity.y * Time.fixedDeltaTime) + groundCheckDistance;
        }

        Vector2 groundRayOrigin = new Vector2(pos.x, pos.y - (playerCollider.bounds.size.y / 2));
        RaycastHit2D groundHit = Physics2D.Raycast(groundRayOrigin, Vector2.down, groundRayLength, GetCurrentMask());

        if (groundHit.collider != null)
        {
            Ground ground = groundHit.collider.GetComponent<Ground>();
            if (ground != null)
            {
                groundHeight = ground.groundHeight;
                float playerBottom = pos.y - (playerCollider.bounds.size.y / 2);

                // Если игрок на уровне земли или ниже, и падает или стоит
                if (playerBottom <= groundHeight + groundCheckDistance && velocity.y <= 0)
                {
                    // Корректируем позицию - ставим точно на землю
                    pos.y = groundHeight + (playerCollider.bounds.size.y / 2);
                    velocity.y = 0;
                    isGrounded = true;

                    // Обработка падающей платформы
                    GroundFall newFall = groundHit.collider.GetComponent<GroundFall>();
                    if (newFall != fall)
                    {
                        if (fall != null)
                        {
                            fall.player = null;
                            cameraController.StopShaking();
                        }
                        fall = newFall;
                        if (fall != null)
                        {
                            fall.player = this;
                            cameraController.StartShaking();
                        }
                    }
                }
                else
                {
                    // Игрок выше земли - он в воздухе
                    isGrounded = false;
                }
            }
            else
            {
                // Объект не является Ground
                isGrounded = false;
            }
        }
        else
        {
            // === КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: Нет земли под ногами - игрок падает! ===
            isGrounded = false;

            if (fall != null)
            {
                fall.player = null;
                fall = null;
                cameraController.StopShaking();
            }
        }

        // === СБРОС СЧЕТЧИКА ПРЫЖКОВ при приземлении ===
        if (isGrounded && !wasGroundedLastFrame)
        {
            currentJumpCount = 0;
        }

        // === ОБРАБОТКА ПРЫЖКА ===
        if (jumpPressed)
        {
            if (currentJumpCount < maxJumpCount)
            {
                float groundDistance = Mathf.Abs(pos.y - (playerCollider.bounds.size.y / 2) - groundHeight);
                bool canJump = false;

                if (currentJumpCount == 0 && (isGrounded || groundDistance <= jumpGroundThreshold))
                {
                    canJump = true;
                }
                else if (currentJumpCount == 1 && canDoubleJump && !isGrounded)
                {
                    canJump = true;
                }

                if (canJump)
                {
                    isGrounded = false;
                    velocity.y = jumpVelocity;

                    if (currentJumpCount == 0)
                    {
                        isHoldingJump = true;
                        holdJumpTimer = 0;
                    }
                    else
                    {
                        isHoldingJump = false;
                    }

                    currentJumpCount++;

                    if (fall != null)
                    {
                        fall.player = null;
                        fall = null;
                        cameraController.StopShaking();
                    }
                }
            }
            jumpPressed = false;
        }

        if (jumpReleased)
        {
            isHoldingJump = false;
            jumpReleased = false;
        }

        // === ПРОВЕРКА СТЕНЫ (когда в воздухе) ===
        if (!isGrounded)
        {
            Vector2 wallOrigin = new Vector2(pos.x, pos.y);
            RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, Vector2.right, velocity.x * Time.fixedDeltaTime, GetCurrentMask());
            if (wallHit.collider != null)
            {
                Ground ground = wallHit.collider.GetComponent<Ground>();
                if (ground != null)
                {
                    float platformTop = wallHit.collider.bounds.max.y;
                    if (pos.y < platformTop)
                    {
                        velocity.x = 0;
                    }
                }
            }
        }

        // === УСКОРЕНИЕ (когда на земле) ===
        if (isGrounded && !isDashing)
        {
            float velocityRatio = velocity.x / maxXVelocity;
            acceleration = maxAcceleration * (1 - velocityRatio);
            maxHoldJumpTime = maxMaxHoldJumpTime;

            velocity.x += acceleration * Time.fixedDeltaTime;
            if (velocity.x >= maxXVelocity)
            {
                velocity.x = maxXVelocity;
            }
        }

        // === ПРОВЕРКА СМЕРТИ ===
        if (pos.y < -20)
        {
            isDead = true;
        }

        // === ДИСТАНЦИЯ ===
        distance += velocity.x * Time.fixedDeltaTime;

        // === ПРОВЕРКА ПРЕПЯТСТВИЙ ===
        Vector2 obstOrigin = new Vector2(pos.x, pos.y);

        RaycastHit2D obstHitX = Physics2D.Raycast(obstOrigin, Vector2.right, velocity.x * Time.fixedDeltaTime, GetCurrentMask());
        if (obstHitX.collider != null)
        {
            Obstacle obstacle = obstHitX.collider.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                hitObstacle(obstacle);
            }
        }

        if (velocity.y != 0)
        {
            Vector2 obstDir = velocity.y > 0 ? Vector2.up : Vector2.down;
            RaycastHit2D obstHitY = Physics2D.Raycast(obstOrigin, obstDir, Mathf.Abs(velocity.y * Time.fixedDeltaTime), GetCurrentMask());
            if (obstHitY.collider != null)
            {
                Obstacle obstacle = obstHitY.collider.GetComponent<Obstacle>();
                if (obstacle != null)
                {
                    hitObstacle(obstacle);
                }
            }
        }

        // === ПРИМЕНЯЕМ ПОЗИЦИЮ ===
        transform.position = pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            // Визуализация raycast для земли
            Gizmos.color = Color.red;
            Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - (playerCollider.bounds.size.y / 2));
            float rayLength = groundCheckDistance;
            if (!isGrounded && velocity.y < 0)
            {
                rayLength = Mathf.Abs(velocity.y * Time.fixedDeltaTime) + groundCheckDistance;
            }
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * rayLength);
            Gizmos.DrawWireSphere(rayOrigin + Vector2.down * rayLength, 0.05f);

            // === НОВОЕ: Визуализация raycast для потолка ===
            Gizmos.color = Color.blue;
            Vector2 ceilingOrigin = new Vector2(transform.position.x, transform.position.y + (playerCollider.bounds.size.y / 2));
            float ceilingRayLength = velocity.y > 0 ? Mathf.Abs(velocity.y * Time.fixedDeltaTime) + ceilingCheckDistance : ceilingCheckDistance;
            Gizmos.DrawLine(ceilingOrigin, ceilingOrigin + Vector2.up * ceilingRayLength);
            Gizmos.DrawWireSphere(ceilingOrigin + Vector2.up * ceilingRayLength, 0.05f);
        }
    }

    public void hitObstacle(Obstacle obstacle)
    {
        isDead = true;
    }

    private void SwitchPolarity()
    {
        currentPolarity = 1 - currentPolarity; // Toggle 0 <-> 1
        UpdateCollisionLayers();
        UpdatePlayerVisuals();
        Debug.Log("Polarity switched to: " + (currentPolarity == 0 ? "Neon" : "Dark"));
    }

    private void UpdateCollisionLayers()
    {
        // Игнорируем коллизии с неподходящим слоем платформ
        Physics2D.IgnoreLayerCollision(playerLayer, neonGroundLayerIndex, currentPolarity != 0);
        Physics2D.IgnoreLayerCollision(playerLayer, darkGroundLayerIndex, currentPolarity != 1);

        // ДОБАВЛЕНО: Игнорируем коллизии с неподходящим слоем шипов
        Physics2D.IgnoreLayerCollision(playerLayer, 10, currentPolarity != 0); // Neon шипы (слой 10)
        Physics2D.IgnoreLayerCollision(playerLayer, 11, currentPolarity != 1); // Dark шипы (слой 11)
    }

    private void UpdatePlayerVisuals()
    {
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.color = (currentPolarity == 0 ? neonPlayerColor : darkPlayerColor);
        }

        // Обновляем обводку, если есть
        SpriteNeonOutline outlineComp = GetComponent<SpriteNeonOutline>();
        if (outlineComp != null)
        {
            outlineComp.outlineTint = (currentPolarity == 0 ? neonPlayerColor : darkPlayerColor);
            // Цвет обновится в LateUpdate() следующего кадра
        }
    }

    private int GetCurrentMask()
    {
        // Обновляем маску чтобы включать оба слоя шипов в зависимости от полярности
        int maskValue = (currentPolarity == 0 ? neonMask : darkMask).value;

        // Добавляем соответствующий слой шипов
        int obstacleLayer = (currentPolarity == 0 ? 10 : 11); // Предполагаемые индексы слоев
        maskValue |= (1 << obstacleLayer);

        return maskValue;
    }

    // ДОБАВЛЕНО: Метод для включения/выключения двойного прыжка
    public void SetDoubleJump(bool enabled)
    {
        canDoubleJump = enabled;
        if (!enabled && currentJumpCount > 0)
        {
            currentJumpCount = Mathf.Min(currentJumpCount, 1);
        }
    }

    // ДОБАВЛЕНО: Метод для получения текущего количества прыжков
    public int GetCurrentJumpCount()
    {
        return currentJumpCount;
    }

    // ДОБАВЛЕНО: Метод для получения максимального количества прыжков
    public int GetMaxJumpCount()
    {
        return maxJumpCount;
    }

    // === МЕТОДЫ ДЛЯ МОНЕТ (обновлённые) ===

    /// <summary>
    /// Добавляет монеты игроку
    /// </summary>
    public void AddCoins(int amount)
    {
        sessionCoins += amount;
        _totalCoins += amount;
        SaveCoins(); // Сохраняем после каждого сбора

        Debug.Log("💰 Coins collected! +" + amount + " | Session: " + sessionCoins + " | Total: " + _totalCoins);
    }

    /// <summary>
    /// Возвращает количество монет в текущей сессии
    /// </summary>
    public int GetSessionCoins()
    {
        return sessionCoins;
    }

    /// <summary>
    /// Возвращает общее количество монет
    /// </summary>
    public int GetTotalCoins()
    {
        return _totalCoins;
    }

    /// <summary>
    /// Тратит монеты (для покупок в магазине)
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (_totalCoins >= amount)
        {
            _totalCoins -= amount;
            SaveCoins();
            Debug.Log("💸 Spent " + amount + " coins. Remaining: " + _totalCoins);
            return true;
        }
        Debug.Log("❌ Not enough coins! Need: " + amount + ", Have: " + _totalCoins);
        return false;
    }

    /// <summary>
    /// Сбрасывает монеты сессии (при рестарте уровня)
    /// </summary>
    public void ResetSessionCoins()
    {
        sessionCoins = 0;
    }

    // === СОХРАНЕНИЕ/ЗАГРУЗКА МОНЕТ ===

    /// <summary>
    /// Сохраняет монеты в PlayerPrefs
    /// </summary>
    private void SaveCoins()
    {
        PlayerPrefs.SetInt(COINS_SAVE_KEY, _totalCoins);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Загружает монеты из PlayerPrefs
    /// </summary>
    private void LoadCoins()
    {
        _totalCoins = PlayerPrefs.GetInt(COINS_SAVE_KEY, 0);
    }

    /// <summary>
    /// Сбрасывает все сохранённые монеты (для отладки)
    /// </summary>
    public void ResetAllCoins()
    {
        _totalCoins = 0;
        sessionCoins = 0;
        PlayerPrefs.DeleteKey(COINS_SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("🗑️ All coins reset!");
    }

    /// <summary>
    /// Добавляет монеты для тестирования (вызывать из консоли или кнопки)
    /// </summary>
    [ContextMenu("Add 100 Test Coins")]
    public void AddTestCoins()
    {
        AddCoins(100);
        Debug.Log("🎁 Added 100 test coins! Total: " + _totalCoins);
    }
}
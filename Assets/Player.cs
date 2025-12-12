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

    // ДОБАВЛЕНО: Переменные для двойного прыжка
    public int maxJumpCount = 2;            // Максимальное количество прыжков
    private int currentJumpCount = 0;       // Текущее количество использованных прыжков
    public bool canDoubleJump = true;       // Можно ли делать двойной прыжок
    private bool wasGrounded = false;       // Был ли игрок на земле в предыдущем кадре


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

    void Start()
    {
        Debug.Log("Player start position: " + transform.position);
        cameraController = Camera.main.GetComponent<CameraController>();

        playerCollider = GetComponent<BoxCollider2D>();

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
        Debug.Log("Dash Action initialized: " + (dashAction != null)); // ТЕСТ
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
            playerSpriteRenderer.color = neonPlayerColor; // Начальное состояние Neon
        }
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
            preDashVelocityX = velocity.x;  // << ФИКС: Сохраняем текущую скорость
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            Debug.Log("🚀 DASH STARTED! Pre-dash velocity.x = " + preDashVelocityX + ", Boost to " + dashBoostSpeed);
        }

        // === ЛОГИКА РЫВКА ===
        if (isDashing)
        {
            velocity.x = dashBoostSpeed;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                velocity.x = preDashVelocityX;  // << КЛЮЧЕВОЙ ФИКС: Возврат к ИСХОДНОЙ скорости!
                Debug.Log("✅ DASH ENDED! velocity.x RESTORED to " + preDashVelocityX);
            }
        }
        else
        {
            // ГЛОБАЛЬНЫЙ CAP: Никогда не выше maxXVelocity (на всякий случай)
            velocity.x = Mathf.Min(velocity.x, maxXVelocity);
            Debug.Log("Cap applied: velocity.x = " + velocity.x + " (grounded=" + isGrounded + ")"); // ТЕСТ, удалите
        }

        Vector2 pos = transform.position;
        float groundDistance = Mathf.Abs(pos.y - groundHeight);

        // СБРОС СЧЕТЧИКА ПРЫЖКОВ при приземлении
        if (isGrounded && !wasGrounded)
        {
            currentJumpCount = 0; // Сбрасываем счетчик прыжков при приземлении
        }
        wasGrounded = isGrounded;

        // ОБРАБОТКА ПРЫЖКА в FixedUpdate
        if (jumpPressed && currentJumpCount < maxJumpCount)
        {
            bool canJump = false;

            // Первый прыжок: можно прыгать с земли или вблизи земли
            if (currentJumpCount == 0 && (isGrounded || groundDistance <= jumpGroundThreshold))
            {
                canJump = true;
            }
            // Второй прыжок: можно прыгать в воздухе
            else if (currentJumpCount == 1 && canDoubleJump)
            {
                canJump = true;
            }

            if (canJump)
            {
                isGrounded = false;
                velocity.y = jumpVelocity;

                // ДЛЯ ВТОРОГО ПРЫЖКА: всегда обычный прыжок (без удержания)
                if (currentJumpCount == 0)
                {
                    isHoldingJump = true;
                    holdJumpTimer = 0;
                }
                else
                {
                    // Второй прыжок - всегда обычный (без возможности удержания)
                    isHoldingJump = false;
                }

                currentJumpCount++;
                jumpPressed = false; // Сбрасываем флаг после использования

                if (fall != null)
                {
                    fall.player = null;
                    fall = null;
                    cameraController.StopShaking();
                }
            }
        }

        if (jumpReleased)
        {
            isHoldingJump = false;
            jumpReleased = false; // Сбрасываем флаг после использования
        }

        Debug.Log("Player position: " + pos + ", isGrounded: " + isGrounded + ", Jumps: " + currentJumpCount);

        if (pos.y < -20)
        {
            Debug.Log("Player died! Position: " + pos.y);
            isDead = true;
        }

        if (!isGrounded)
        {
            if (isHoldingJump)
            {
                holdJumpTimer += Time.fixedDeltaTime;
                if (holdJumpTimer >= maxHoldJumpTime)
                {
                    isHoldingJump = false;
                }
            }

            pos.y += velocity.y * Time.fixedDeltaTime;
            if (!isHoldingJump)
            {
                velocity.y += gravity * Time.fixedDeltaTime;
            }

            // ПРОВЕРКА ЗЕМЛИ ПОД ИГРОКОМ С ПОМОЩЬЮ RAYCAST
            float rayLength = groundCheckDistance;
            if (velocity.y < 0)
            {
                rayLength = Mathf.Abs(velocity.y * Time.fixedDeltaTime) + groundCheckDistance;
            }

            Vector2 rayOrigin = new Vector2(pos.x, pos.y - (playerCollider.bounds.size.y / 2));
            RaycastHit2D groundHit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, GetCurrentMask());

            if (groundHit.collider != null)
            {
                Ground ground = groundHit.collider.GetComponent<Ground>();
                if (ground != null)
                {
                    groundHeight = ground.groundHeight;
                    pos.y = groundHeight + (playerCollider.bounds.size.y / 2);
                    velocity.y = 0;
                    isGrounded = true;

                    fall = groundHit.collider.GetComponent<GroundFall>();
                    if (fall != null)
                    {
                        fall.player = this;
                        cameraController.StartShaking();
                    }
                }
            }
            else
            {
                isGrounded = false;
            }

            Vector2 wallOrigin = new Vector2(pos.x, pos.y);
            Vector2 wallDir = Vector2.right;
            RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, wallDir, velocity.x * Time.fixedDeltaTime, GetCurrentMask());
            if (wallHit.collider != null)
            {
                Ground ground = wallHit.collider.GetComponent<Ground>();
                if (ground != null)
                {
                    // Проверяем, находится ли игрок НИЖЕ верхней части платформы
                    float platformTop = wallHit.collider.bounds.max.y;
                    if (pos.y < platformTop)
                    {
                        // Игрок ударился о бок платформы - сбрасываем скорость
                        velocity.x = 0;
                    }
                    else
                    {
                        // Игрок ПЕРЕПРЫГНУЛ платформу - скорость не сбрасываем
                        // Можно добавить небольшой буфер для надежности
                    }
                }
            }
        }

        distance += velocity.x * Time.fixedDeltaTime;

        if (isGrounded && !isDashing)
        {
            float velocityRatio = velocity.x / maxXVelocity;
            acceleration = maxAcceleration * (1 - velocityRatio);

            // СИЛА ПРЫЖКА ПОСТОЯННАЯ - не зависит от скорости
            maxHoldJumpTime = maxMaxHoldJumpTime;

            velocity.x += acceleration * Time.fixedDeltaTime;
            if (velocity.x >= maxXVelocity)
            {
                velocity.x = maxXVelocity;
            }

            // ПОВТОРНАЯ ПРОВЕРКА ЗЕМЛИ ПОД ИГРОКОМ С ПОМОЩЬЮ RAYCAST
            Vector2 rayOrigin = new Vector2(pos.x, pos.y - (playerCollider.bounds.size.y / 2));
            RaycastHit2D groundHit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, GetCurrentMask());

            if (groundHit.collider == null)
            {
                isGrounded = false;
                if (fall != null)
                {
                    fall.player = null;
                    fall = null;
                    cameraController.StopShaking();
                }
            }
        }

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

        RaycastHit2D obstHitY = Physics2D.Raycast(obstOrigin, Vector2.up, velocity.y * Time.fixedDeltaTime, GetCurrentMask());
        if (obstHitY.collider != null)
        {
            Obstacle obstacle = obstHitY.collider.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                hitObstacle(obstacle);
            }
        }

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
        // Игнорируем коллизии с неподходящим слоем
        Physics2D.IgnoreLayerCollision(playerLayer, neonGroundLayerIndex, currentPolarity != 0);
        Physics2D.IgnoreLayerCollision(playerLayer, darkGroundLayerIndex, currentPolarity != 1);
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
        return (currentPolarity == 0 ? neonMask : darkMask).value;
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
}
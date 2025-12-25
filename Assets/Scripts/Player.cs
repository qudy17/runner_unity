using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // Основные параметры
    public float gravity;
    public Vector2 velocity;
    public float maxXVelocity = 100;
    public float maxAcceleration = 10;
    public float acceleration = 10;
    public float distance = 0;
    public float jumpVelocity = 20;
    public float groundHeight = 10;
    public bool isGrounded = false;

    public bool isHoldingJump = false;
    public float maxHoldJumpTime = 0.4f;
    public float maxMaxHoldJumpTime = 0.4f;
    public float holdJumpTimer = 0.0f;

    public float jumpGroundThreshold = 1;

    public bool isDead = false;

    // Система монет
    [Header("Coin System")]
    public int sessionCoins = 0;
    private int _totalCoins = 1000;
    public int totalCoins
    {
        get { return _totalCoins; }
        set
        {
            _totalCoins = value;
            SaveCoinsToJson();
        }
    }

    // Двойной прыжок
    [Header("Jump Settings")]
    public int maxJumpCount = 2;
    private int currentJumpCount = 0;
    public bool canDoubleJump = true;

    // Полярность
    [Header("Polarity Settings")]
    public int currentPolarity = 0;
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

    // Скины
    [Header("Skin Settings")]
    public RuntimeAnimatorController punkSkin;
    public RuntimeAnimatorController cyborgSkin;
    public RuntimeAnimatorController bikerSkin;
    private Animator playerAnimator;
    private string currentAppliedSkin = "";

    GroundFall fall;
    CameraController cameraController;

    // Input System
    private PlayerInput playerInput;
    private InputAction jumpAction;
    private bool jumpPressed = false;
    private bool jumpReleased = false;

    // Физика
    private BoxCollider2D playerCollider;
    public float groundCheckDistance = 0.1f;

    // Рывок
    public float dashBoostSpeed = 250f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f; // Базовый кулдаун
    public float airDeceleration = 5f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private bool dashPressed = false;

    private InputAction dashAction;
    private float preDashVelocityX = 0f;

    // Переключение полярности
    private InputAction switchPolarityAction;

    // Потолок
    [Header("Ceiling Settings")]
    public LayerMask ceilingMask;
    public float ceilingCheckDistance = 0.2f;

    void Start()
    {
        // Загружаем монеты
        LoadCoinsFromJson();

        Debug.Log("Player start. Total coins: " + _totalCoins);

        ApplyMultiplierFromSave();

        // Получаем компоненты
        cameraController = Camera.main.GetComponent<CameraController>();
        playerCollider = GetComponent<BoxCollider2D>();
        playerAnimator = GetComponent<Animator>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();

        // Настройка стартовой позиции
        Vector2 pos = transform.position;
        Vector2 groundRayOrigin = new Vector2(pos.x, pos.y - (playerCollider.bounds.size.y / 2));
        int startMask = (1 << neonGroundLayerIndex) | (1 << darkGroundLayerIndex);
        RaycastHit2D groundHit = Physics2D.Raycast(groundRayOrigin, Vector2.down, 1f, startMask);

        if (groundHit.collider != null)
        {
            Ground ground = groundHit.collider.GetComponent<Ground>();
            if (ground != null)
            {
                groundHeight = ground.groundHeight;
                pos.y = groundHeight + (playerCollider.bounds.size.y / 2);
                transform.position = pos;
                isGrounded = true;
            }
        }

        // Input System
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

        switchPolarityAction = playerInput.actions["SwitchPolarity"];
        if (switchPolarityAction != null)
        {
            switchPolarityAction.started += OnPolaritySwitch;
        }

        // Настройка слоёв
        neonMask = 1 << neonGroundLayerIndex;
        darkMask = 1 << darkGroundLayerIndex;
        playerLayer = gameObject.layer;

        // Применяем скин из сохранения
        ApplySkinFromSave();

        // Применяем улучшения из сохранения
        ApplyUpgradesFromSave();

        // Начальная настройка графики
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.color = neonPlayerColor;
        }

        UpdateCollisionLayers();
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

    [Header("Score Multiplier")]
    public float scoreMultiplier = 1.0f; // Текущий множитель
    private int _scoreMultiplierLevel = 0; // Уровень множителя

    public int scoreMultiplierLevel
    {
        get { return _scoreMultiplierLevel; }
        set
        {
            _scoreMultiplierLevel = Mathf.Clamp(value, 0, 10);
            scoreMultiplier = 1.0f + (_scoreMultiplierLevel * 0.1f); // +10% за уровень
        }
    }

    void ApplyMultiplierFromSave()
    {
        PlayerSaveData saveData = UIController.Instance?.GetSaveData();
        if (saveData == null) return;

        scoreMultiplierLevel = saveData.scoreMultiplierLevel;
        Debug.Log("Score multiplier level: " + scoreMultiplierLevel + " (x" + scoreMultiplier + ")");
    }

    public float GetScoreMultiplier()
    {
        return scoreMultiplier;
    }

    public int GetScoreMultiplierLevel()
    {
        return scoreMultiplierLevel;
    }

    public void SetScoreMultiplierLevel(int level)
    {
        scoreMultiplierLevel = level;

        // Сохраняем в сохранение
        PlayerSaveData saveData = UIController.Instance?.GetSaveData();
        if (saveData != null)
        {
            saveData.scoreMultiplierLevel = scoreMultiplierLevel;
            UIController.Instance.UpdateSaveData(saveData);
        }
    }

    void ApplySkinFromSave()
    {
        if (playerAnimator == null) return;

        PlayerSaveData saveData = UIController.Instance?.GetSaveData();
        if (saveData == null) return;

        string skin = saveData.selectedSkin;

        switch (skin)
        {
            case "Punk":
                if (punkSkin != null)
                {
                    playerAnimator.runtimeAnimatorController = punkSkin;
                    currentAppliedSkin = "Punk";
                }
                break;
            case "Cyborg":
                if (cyborgSkin != null)
                {
                    playerAnimator.runtimeAnimatorController = cyborgSkin;
                    currentAppliedSkin = "Cyborg";
                }
                break;
            case "Biker":
                if (bikerSkin != null)
                {
                    playerAnimator.runtimeAnimatorController = bikerSkin;
                    currentAppliedSkin = "Biker";
                }
                break;
        }

        Debug.Log("Applied skin: " + skin);
    }

    void ApplyUpgradesFromSave()
    {
        PlayerSaveData saveData = UIController.Instance?.GetSaveData();
        if (saveData == null) return;

        // Применяем улучшение рывка
        if (saveData.dashUpgradeLevel > 0)
        {
            dashCooldown = 0.8f - (saveData.dashUpgradeLevel * 0.2f);
            Debug.Log("Dash cooldown upgraded to: " + dashCooldown + "s");
        }

        // Применяем двойной прыжок если куплен
        if (saveData.doubleJumpUnlocked)
        {
            canDoubleJump = true;
            maxJumpCount = 2;
            Debug.Log("Double jump unlocked");
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
    }

    private void OnPolaritySwitch(InputAction.CallbackContext context)
    {
        SwitchPolarity();
    }

    void Update()
    {
        // Только сбор input
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Обновление кулдауна рывка
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.fixedDeltaTime);

        // Рывок
        if (dashPressed && dashCooldownTimer <= 0f && !isDashing)
        {
            dashPressed = false;
            preDashVelocityX = velocity.x;
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }

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
        bool wasGroundedLastFrame = isGrounded;

        // Вертикальное движение
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

            if (!isHoldingJump)
            {
                velocity.y += gravity * Time.fixedDeltaTime;
            }

            // Проверка потолка
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

            pos.y += velocity.y * Time.fixedDeltaTime;
        }

        // Проверка земли
        float groundRayLength = groundCheckDistance;
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

                if (playerBottom <= groundHeight + groundCheckDistance && velocity.y <= 0)
                {
                    pos.y = groundHeight + (playerCollider.bounds.size.y / 2);
                    velocity.y = 0;
                    isGrounded = true;

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
                    isGrounded = false;
                }
            }
            else
            {
                isGrounded = false;
            }
        }
        else
        {
            isGrounded = false;
            if (fall != null)
            {
                fall.player = null;
                fall = null;
                cameraController.StopShaking();
            }
        }

        // Сброс прыжков при приземлении
        if (isGrounded && !wasGroundedLastFrame)
        {
            currentJumpCount = 0;
        }

        // Прыжок
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

        // Проверка стен
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

        // Ускорение на земле
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

        // Смерть
        if (pos.y < -20)
        {
            isDead = true;
        }

        // Дистанция
        distance += velocity.x * Time.fixedDeltaTime;

        // Проверка препятствий
        CheckObstacleCollisions(pos);

        // Применение позиции
        transform.position = pos;

        // Обновление анимации
        UpdateAnimation();
    }

    void CheckObstacleCollisions(Vector2 pos)
    {
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
    }

    void UpdateAnimation()
    {
        if (playerAnimator == null) return;

        playerAnimator.SetBool("IsRunning", velocity.x > 0.1f);
        playerAnimator.SetBool("IsGrounded", isGrounded);
        playerAnimator.SetFloat("VelocityY", velocity.y);
        playerAnimator.SetBool("IsDashing", isDashing);
    }

    public void hitObstacle(Obstacle obstacle)
    {
        isDead = true;
    }

    private void SwitchPolarity()
    {
        currentPolarity = 1 - currentPolarity;
        UpdateCollisionLayers();
        UpdatePlayerVisuals();
    }

    private void UpdateCollisionLayers()
    {
        Physics2D.IgnoreLayerCollision(playerLayer, neonGroundLayerIndex, currentPolarity != 0);
        Physics2D.IgnoreLayerCollision(playerLayer, darkGroundLayerIndex, currentPolarity != 1);
        Physics2D.IgnoreLayerCollision(playerLayer, 10, currentPolarity != 0);
        Physics2D.IgnoreLayerCollision(playerLayer, 11, currentPolarity != 1);
    }

    private void UpdatePlayerVisuals()
    {
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.color = (currentPolarity == 0 ? neonPlayerColor : darkPlayerColor);
        }

        SpriteNeonOutline outlineComp = GetComponent<SpriteNeonOutline>();
        if (outlineComp != null)
        {
            outlineComp.outlineTint = (currentPolarity == 0 ? neonPlayerColor : darkPlayerColor);
        }
    }

    private int GetCurrentMask()
    {
        int maskValue = (currentPolarity == 0 ? neonMask : darkMask).value;
        int obstacleLayer = (currentPolarity == 0 ? 10 : 11);
        maskValue |= (1 << obstacleLayer);
        return maskValue;
    }

    // === МЕТОДЫ ДЛЯ МОНЕТ ===

    public void AddCoins(int amount)
    {
        sessionCoins += amount;
        _totalCoins += amount;
        SaveCoinsToJson();
        Debug.Log("💰 Coins collected! +" + amount);
    }

    public int GetSessionCoins()
    {
        return sessionCoins;
    }

    public int GetTotalCoins()
    {
        return _totalCoins;
    }

    public void SetTotalCoins(int amount)
    {
        _totalCoins = amount;
        SaveCoinsToJson();
    }

    public bool SpendCoins(int amount)
    {
        if (_totalCoins >= amount)
        {
            _totalCoins -= amount;
            SaveCoinsToJson();
            return true;
        }
        return false;
    }

    public void ResetSessionCoins()
    {
        sessionCoins = 0;
    }

    // Сохранение/загрузка
    private void SaveCoinsToJson()
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.UpdateCoinSaveData(_totalCoins);
        }
        else
        {
            Debug.LogWarning("UIController not found! Coins not saved.");
        }
    }

    private void LoadCoinsFromJson()
    {
        if (UIController.Instance != null)
        {
            _totalCoins = UIController.Instance.GetSavedCoins();
        }
        else
        {
            _totalCoins = 0;
            Debug.LogWarning("UIController not found! Starting with 0 coins.");
        }
    }

    // Двойной прыжок
    public void SetDoubleJump(bool enabled)
    {
        canDoubleJump = enabled;
        if (!enabled && currentJumpCount > 0)
        {
            currentJumpCount = Mathf.Min(currentJumpCount, 1);
        }
    }

    public int GetCurrentJumpCount()
    {
        return currentJumpCount;
    }

    public int GetMaxJumpCount()
    {
        return maxJumpCount;
    }

    public int GetMultipliedDistance()
    {
        float multipliedDistance = distance * scoreMultiplier;
        return Mathf.FloorToInt(multipliedDistance);
    }

    // Старый метод оставляем для совместимости
    public int GetDistance()
    {
        return Mathf.FloorToInt(distance);
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Gizmos.color = Color.red;
            Vector2 rayOrigin = new Vector2(transform.position.x, transform.position.y - (playerCollider.bounds.size.y / 2));
            float rayLength = groundCheckDistance;
            if (!isGrounded && velocity.y < 0)
            {
                rayLength = Mathf.Abs(velocity.y * Time.fixedDeltaTime) + groundCheckDistance;
            }
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * rayLength);
            Gizmos.DrawWireSphere(rayOrigin + Vector2.down * rayLength, 0.05f);

            Gizmos.color = Color.blue;
            Vector2 ceilingOrigin = new Vector2(transform.position.x, transform.position.y + (playerCollider.bounds.size.y / 2));
            float ceilingRayLength = velocity.y > 0 ? Mathf.Abs(velocity.y * Time.fixedDeltaTime) + ceilingCheckDistance : ceilingCheckDistance;
            Gizmos.DrawLine(ceilingOrigin, ceilingOrigin + Vector2.up * ceilingRayLength);
            Gizmos.DrawWireSphere(ceilingOrigin + Vector2.up * ceilingRayLength, 0.05f);
        }
    }
}
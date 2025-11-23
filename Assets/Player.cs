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

    public LayerMask groundLayerMask;
    public LayerMask obstacleLayerMask;

    GroundFall fall;
    CameraController cameraController;

    private PlayerInput playerInput;
    private InputAction jumpAction;
    private bool jumpPressed = false;
    private bool jumpReleased = false;

    private BoxCollider2D playerCollider;
    public float groundCheckDistance = 0.1f;

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
    }

    void OnDestroy()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpStarted;
            jumpAction.canceled -= OnJumpCanceled;
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
            RaycastHit2D groundHit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, groundLayerMask);

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
            RaycastHit2D wallHit = Physics2D.Raycast(wallOrigin, wallDir, velocity.x * Time.fixedDeltaTime, groundLayerMask);
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

        if (isGrounded)
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
            RaycastHit2D groundHit = Physics2D.Raycast(rayOrigin, Vector2.down, groundCheckDistance, groundLayerMask);

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
        RaycastHit2D obstHitX = Physics2D.Raycast(obstOrigin, Vector2.right, velocity.x * Time.fixedDeltaTime, obstacleLayerMask);
        if (obstHitX.collider != null)
        {
            Obstacle obstacle = obstHitX.collider.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                hitObstacle(obstacle);
            }
        }

        RaycastHit2D obstHitY = Physics2D.Raycast(obstOrigin, Vector2.up, velocity.y * Time.fixedDeltaTime, obstacleLayerMask);
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float jumpForce = 10f;
    private Rigidbody2D rb;
    private bool isGrounded;
    private PlayerInput playerInput;
    public Animator anim;

    // Переменные для двойного прыжка
    private int jumpsRemaining;
    public int maxJumps = 2;

    // Переменная для гравитации
    public float gravityScale = 3f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        jumpsRemaining = maxJumps;

        // Устанавливаем гравитацию
        rb.gravityScale = gravityScale;
    }

    void Update()
    {
        // Двойной прыжок через Input System
        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpsRemaining > 0)
        {
            PerformJump();
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            // Проверяем наличие параметра перед установкой
            if (anim != null && AnimatorHasParameter("Slide"))
            {
                anim.SetTrigger("Slide");
            }
        }
    }

    // Метод для выполнения прыжка
    private void PerformJump()
    {
        // Сбрасываем вертикальную скорость для более предсказуемого прыжка
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // Применяем силу прыжка
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // Уменьшаем количество оставшихся прыжков
        jumpsRemaining--;

        // Анимация прыжка (только если параметр существует)
        if (anim != null && AnimatorHasParameter("Jump"))
        {
            anim.SetTrigger("Jump");
        }
    }

    // Через Input Actions
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && jumpsRemaining > 0)
        {
            PerformJump();
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            jumpsRemaining = maxJumps;

            // Анимация приземления (только если параметр существует)
            if (anim != null && AnimatorHasParameter("IsGrounded"))
            {
                anim.SetBool("IsGrounded", true);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            if (anim != null && AnimatorHasParameter("IsGrounded"))
            {
                anim.SetBool("IsGrounded", false);
            }
        }
    }

    // Метод для проверки существования параметра в аниматоре
    private bool AnimatorHasParameter(string paramName)
    {
        if (anim == null) return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    // Дополнительный метод для сброса прыжков
    public void ResetJumps()
    {
        jumpsRemaining = maxJumps;
    }

    // Для отладки
    public int GetRemainingJumps()
    {
        return jumpsRemaining;
    }

    // Для отладки в консоли
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), $"Jumps: {jumpsRemaining}/{maxJumps}");
        GUI.Label(new Rect(10, 30, 200, 20), $"Grounded: {isGrounded}");
    }
}
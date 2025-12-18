using UnityEngine;

public class SpriteNeonOutline : MonoBehaviour
{
    [Header("Настройки обводки")]
    public Color outlineTint = Color.white;  // Tint обводки (обычно white, 0-1)
    public float outlineSize = 0.05f;        // Толщина обводки
    public Material outlineMaterial;         // Назначьте PlayerNeonOutline.mat!

    private SpriteRenderer mainRenderer;
    private Transform outlineTransform;
    private SpriteRenderer outlineRenderer;
    private Animator mainAnimator;
    private Animator outlineAnimator;

    void Start()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        if (mainRenderer == null)
        {
            Debug.LogError("SpriteRenderer не найден на " + gameObject.name);
            return;
        }

        if (outlineMaterial == null)
        {
            Debug.LogError("Назначьте PlayerNeonOutline.mat в поле Outline Material!");
            return;
        }

        // Создаём обводку
        CreateOutline();

        // Сохраняем ссылки для анимации
        mainAnimator = GetComponent<Animator>();
        if (mainAnimator != null)
        {
            outlineAnimator = outlineTransform.GetComponent<Animator>();
        }
    }

    void CreateOutline()
    {
        // Удаляем старую обводку, если есть
        if (outlineTransform != null) DestroyImmediate(outlineTransform.gameObject);

        GameObject outline = new GameObject("NeonOutline");
        outlineTransform = outline.transform;
        outlineTransform.SetParent(transform);
        outlineTransform.localPosition = Vector3.zero;
        outlineTransform.localRotation = Quaternion.identity;
        outlineTransform.localScale = Vector3.one * (1 + outlineSize);

        outlineRenderer = outline.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = mainRenderer.sprite;
        outlineRenderer.color = outlineTint;  // Белый tint + яркий _Color мат = неон
        outlineRenderer.material = new Material(outlineMaterial);  // Копия для независимости
        outlineRenderer.sortingOrder = mainRenderer.sortingOrder - 1;  // Сзади основного

        // Анимация обводки
        if (mainAnimator != null)
        {
            outlineAnimator = outline.AddComponent<Animator>();
            outlineAnimator.runtimeAnimatorController = mainAnimator.runtimeAnimatorController;
        }
    }

    void LateUpdate()  // LateUpdate для синхронизации с анимацией
    {
        if (outlineRenderer == null || mainRenderer == null) return;

        // Синхронизируем спрайт и анимацию
        outlineRenderer.sprite = mainRenderer.sprite;
        outlineRenderer.color = outlineTint;

        if (mainAnimator != null && outlineAnimator != null)
        {
            // Копируем все параметры анимации (speed, etc.)
            foreach (var param in mainAnimator.parameters)
            {
                outlineAnimator.SetFloat(param.name, mainAnimator.GetFloat(param.name));
                outlineAnimator.SetBool(param.name, mainAnimator.GetBool(param.name));
                outlineAnimator.SetInteger(param.name, mainAnimator.GetInteger(param.name));
                outlineAnimator.SetTrigger(param.name);  // Triggers копируются автоматически
            }
        }
    }

    void OnDestroy()
    {
        if (outlineTransform != null) Destroy(outlineTransform.gameObject);
    }
}
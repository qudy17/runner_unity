using UnityEngine;

public class SpriteNeonOutline : MonoBehaviour
{
    public Color outlineColor = Color.cyan;
    public float outlineSize = 0.05f;

    void Start()
    {
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(transform);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localScale = Vector3.one * (1 + outlineSize); // УВЕЛИЧИВАЕМ размер!

        SpriteRenderer outlineRenderer = outline.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = GetComponent<SpriteRenderer>().sprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.sortingOrder = 1; // Позади основного спрайта

        // Делаем материал с свечением
        Material outlineMaterial = new Material(Shader.Find("Sprites/Default"));
        outlineMaterial.SetColor("_Color", outlineColor);
        outlineMaterial.SetFloat("_Glow", 1f); // Добавляем свечение
        outlineRenderer.material = outlineMaterial;
    }
}
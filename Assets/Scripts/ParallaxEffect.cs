using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layerTransform;
        public float scrollSpeed;
        public bool isLooping;

        [HideInInspector] public Vector3 initialPosition;
        [HideInInspector] public Vector3 initialScale;
        [HideInInspector] public Transform cloneTransform; // Клон спрайта
        [HideInInspector] public float spriteWidth;
    }

    public ParallaxLayer[] layers;
    public bool invertDirection = false;

    void Start()
    {
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.layerTransform != null)
            {
                layer.initialPosition = layer.layerTransform.position;
                layer.initialScale = layer.layerTransform.localScale;

                if (layer.isLooping)
                {
                    SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        layer.spriteWidth = sr.bounds.size.x;

                        // Создаём клон справа от оригинала
                        GameObject clone = Instantiate(
                            layer.layerTransform.gameObject,
                            layer.layerTransform.parent
                        );
                        clone.name = layer.layerTransform.name + "_Clone";
                        layer.cloneTransform = clone.transform;

                        // Позиционируем клон справа
                        Vector3 clonePos = layer.layerTransform.position;
                        clonePos.x += layer.spriteWidth;
                        layer.cloneTransform.position = clonePos;

                        // Удаляем ParallaxEffect с клона, если есть
                        ParallaxEffect cloneParallax = clone.GetComponent<ParallaxEffect>();
                        if (cloneParallax != null)
                        {
                            Destroy(cloneParallax);
                        }
                    }
                }
            }
        }
    }

    void Update()
    {
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.layerTransform == null) continue;

            float effectiveSpeed = layer.scrollSpeed * (invertDirection ? -1f : 1f);
            float movement = effectiveSpeed * Time.deltaTime;

            // Двигаем основной спрайт
            Vector3 newPosition = layer.layerTransform.position;
            newPosition.x += movement;
            newPosition.y = layer.initialPosition.y;
            newPosition.z = layer.initialPosition.z;

            if (layer.isLooping && layer.cloneTransform != null)
            {
                // Двигаем клон
                Vector3 clonePosition = layer.cloneTransform.position;
                clonePosition.x += movement;
                clonePosition.y = layer.initialPosition.y;
                clonePosition.z = layer.initialPosition.z;

                // Проверяем перемещение (движение влево - скорость отрицательная)
                if (effectiveSpeed < 0)
                {
                    // Если основной спрайт ушёл за левую границу
                    if (newPosition.x <= -layer.spriteWidth)
                    {
                        newPosition.x = clonePosition.x + layer.spriteWidth;
                    }
                    // Если клон ушёл за левую границу
                    if (clonePosition.x <= -layer.spriteWidth)
                    {
                        clonePosition.x = newPosition.x + layer.spriteWidth;
                    }
                }
                else // Движение вправо
                {
                    // Если основной спрайт ушёл за правую границу
                    if (newPosition.x >= layer.spriteWidth)
                    {
                        newPosition.x = clonePosition.x - layer.spriteWidth;
                    }
                    // Если клон ушёл за правую границу
                    if (clonePosition.x >= layer.spriteWidth)
                    {
                        clonePosition.x = newPosition.x - layer.spriteWidth;
                    }
                }

                layer.cloneTransform.position = clonePosition;
            }

            layer.layerTransform.position = newPosition;
        }
    }

    void OnDestroy()
    {
        // Очистка клонов при уничтожении
        foreach (ParallaxLayer layer in layers)
        {
            if (layer.cloneTransform != null)
            {
                Destroy(layer.cloneTransform.gameObject);
            }
        }
    }
}
using UnityEngine;
using System.Collections;

public class StarShapeAnimator : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Vector3[] originalVertices;

    [Header("Wave Animation")]
    public float waveSpeed = 2f;
    public float waveAmplitude = 0.2f;
    public float waveFrequency = 3f;

    [Header("Twist Animation")]
    public float twistSpeed = 1f;
    public float twistAmount = 15f;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
        {
            originalVertices = meshFilter.mesh.vertices;
        }

        StartCoroutine(AnimateShape());
    }

    System.Collections.IEnumerator AnimateShape()
    {
        while (true)
        {
            if (meshFilter != null && originalVertices != null)
            {
                Vector3[] vertices = (Vector3[])originalVertices.Clone();

                float time = Time.time;

                // Волновая деформация лучей
                for (int i = 1; i < vertices.Length; i++)
                {
                    // Пропускаем центральную вершину
                    if (i == 0) continue;

                    Vector3 vertex = originalVertices[i];
                    float distanceFromCenter = vertex.magnitude;
                    float angle = Mathf.Atan2(vertex.y, vertex.x);

                    // Волна
                    float wave = Mathf.Sin(time * waveSpeed + distanceFromCenter * waveFrequency) * waveAmplitude;

                    // Скручивание
                    float twist = Mathf.Sin(time * twistSpeed + angle * twistAmount) * 0.1f;

                    // Применяем деформации
                    Vector3 direction = vertex.normalized;
                    vertices[i] = vertex + direction * wave;

                    // Поворот
                    float rotatedAngle = angle + twist;
                    vertices[i] = new Vector3(
                        Mathf.Cos(rotatedAngle) * distanceFromCenter,
                        Mathf.Sin(rotatedAngle) * distanceFromCenter,
                        0
                    );
                }

                meshFilter.mesh.vertices = vertices;
                meshFilter.mesh.RecalculateNormals();
            }

            yield return null;
        }
    }
}
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    Player player;

    private void Awake()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
    }

    void Start()
    {

    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (player == null || player.isDead)
            return;

        Vector2 pos = transform.position;

        pos.x -= player.velocity.x * Time.fixedDeltaTime;
        if (pos.x < -20)
        {
            Destroy(gameObject);
        }

        transform.position = pos;
    }

    // ДОБАВЬТЕ ЭТОТ МЕТОД для обработки столкновений
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null && !player.isDead)
            {
                player.hitObstacle(this);
            }
        }
    }
}
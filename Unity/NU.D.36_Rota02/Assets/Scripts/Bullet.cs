using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private Vector3 direction = Vector3.forward;
    [SerializeField] private float speed = 40f;
    [SerializeField] private float lifetime = 5f;

    public void Launch(Vector3 fireDirection, float bulletSpeed = 40f, float life = 5f)
    {
        direction = fireDirection.normalized;
        speed = bulletSpeed;
        lifetime = life;
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}

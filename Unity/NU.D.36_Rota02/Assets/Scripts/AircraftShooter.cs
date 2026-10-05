using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AircraftShooter : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 40f;
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private GameObject bulletPrefab;

    public float BulletSpeed
    {
        get => bulletSpeed;
        set => bulletSpeed = value;
    }

    private void Update()
    {
        bool shootRequested = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            shootRequested = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (!shootRequested)
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    shootRequested = true;
                }
            }
            catch (System.InvalidOperationException)
            {
                // Fallback when active input handler is set to Input System package
            }
        }
#endif

        if (shootRequested)
        {
            FireBullet();
        }
    }

    public GameObject FireBullet()
    {
        GameObject bulletObj;
        if (bulletPrefab != null)
        {
            bulletObj = Instantiate(bulletPrefab, transform.position + transform.forward * 3f, Quaternion.LookRotation(transform.forward));
        }
        else
        {
            bulletObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletObj.name = "Bullet";
            bulletObj.transform.position = transform.position + transform.forward * 3f;
            bulletObj.transform.localScale = Vector3.one * 0.4f;

            Collider col = bulletObj.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
        }

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = bulletObj.AddComponent<Bullet>();
        }

        bullet.Launch(transform.forward, bulletSpeed, bulletLifetime);
        return bulletObj;
    }
}

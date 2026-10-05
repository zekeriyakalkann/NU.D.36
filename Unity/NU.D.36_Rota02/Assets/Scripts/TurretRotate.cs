using UnityEngine;

public class TurretRotate : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 30f;

    public float RotationSpeed
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}

using UnityEngine;

public class Radar : MonoBehaviour
{
    [SerializeField] private Transform hedef;
    [SerializeField] private float halfAngle = 35f;
    [SerializeField] private float range = 120f;

    [SerializeField] private bool hedefGorunuyor = false;
    [SerializeField] private float sonMesafe = 0f;
    [SerializeField] private float sonDot = 0f;

    public bool HedefGorunuyor => hedefGorunuyor;
    public float SonMesafe => sonMesafe;
    public float SonDot => sonDot;

    private void Update()
    {
        if (hedef == null)
        {
            hedefGorunuyor = false;
            return;
        }

        Vector3 toTarget = hedef.position - transform.position;
        float distance = toTarget.magnitude;
        sonMesafe = distance;

        if (distance <= range)
        {
            Vector3 directionToTarget = toTarget.normalized;
            float dot = Vector3.Dot(transform.forward, directionToTarget);
            sonDot = dot;

            float cosHalfAngle = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

            hedefGorunuyor = dot >= cosHalfAngle;
        }
        else
        {
            sonDot = 0f;
            hedefGorunuyor = false;
        }
    }

    private void OnDrawGizmos()
    {
        // 1. Radar range wire sphere centered on aircraft
        Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, range);

        // 2. Aircraft forward direction (blue line)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * range);

        // 3. Horizontal FOV boundaries using halfAngle and Quaternion.AngleAxis around transform.up
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, transform.up) * transform.forward;
        Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, transform.up) * transform.forward;
        Gizmos.DrawRay(transform.position, leftBoundary * range);
        Gizmos.DrawRay(transform.position, rightBoundary * range);

        // BONUS 4: FOV cone visual representation using rays & perimeter arc around transform.up
        int coneSteps = 14;
        Vector3 prevPoint = transform.position + leftBoundary * range;
        for (int i = 1; i <= coneSteps; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / coneSteps);
            Vector3 coneDir = Quaternion.AngleAxis(angle, transform.up) * transform.forward;
            Vector3 currentPoint = transform.position + coneDir * range;

            // Interior FOV rays
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.35f);
            Gizmos.DrawRay(transform.position, coneDir * range);

            // Perimeter arc connection
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(prevPoint, currentPoint);

            prevPoint = currentPoint;
        }

        // 4. Direction from aircraft toward target (green if visible, red if not)
        if (hedef != null)
        {
            bool isVisible = hedefGorunuyor;
            if (!Application.isPlaying)
            {
                Vector3 toTarget = hedef.position - transform.position;
                float dist = toTarget.magnitude;
                if (dist <= range && dist > 0.001f)
                {
                    float dot = Vector3.Dot(transform.forward, toTarget.normalized);
                    isVisible = dot >= Mathf.Cos(halfAngle * Mathf.Deg2Rad);
                }
                else
                {
                    isVisible = false;
                }
            }

            Gizmos.color = isVisible ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, hedef.position);
        }
    }
}

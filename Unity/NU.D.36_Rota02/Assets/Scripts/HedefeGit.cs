using UnityEngine;

public class HedefeGit : MonoBehaviour
{
    [SerializeField] private Transform hedef;
    [SerializeField] private float hiz = 14f;
    [SerializeField] private float donusHizi = 60f;
    [SerializeField] private float varisMesafesi = 8f;

    private void Update()
    {
        if (hedef == null) return;

        Vector3 fark = hedef.position - transform.position;
        float mesafe = fark.magnitude;

        if (mesafe < varisMesafesi) return;

        Vector3 yon = fark.normalized;

        // Lateral relationship via Cross Product
        float yan = Vector3.Cross(transform.forward, yon).y;

        // Target heading rotation toward target direction
        Quaternion hedefDonusu = Quaternion.LookRotation(yon);

        // Current heading isolated from roll (forward vector with world up)
        Quaternion currentHeading = Quaternion.LookRotation(transform.forward, Vector3.up);

        // Step heading smoothly toward target
        Quaternion nextHeading = Quaternion.RotateTowards(
            currentHeading,
            hedefDonusu,
            donusHizi * Time.deltaTime
        );

        // Apply bank angle around forward axis without accumulation
        // Right target (yan > 0) -> negative roll -> right wing down
        // Left target (yan < 0) -> positive roll -> left wing down
        // Aligned (yan -> 0) -> zero roll -> returns to level
        transform.rotation = nextHeading * Quaternion.Euler(0f, 0f, -yan * 45f);

        // Move forward along aircraft forward vector
        transform.position += transform.forward * hiz * Time.deltaTime;
    }
}

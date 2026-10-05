using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseTargeting : MonoBehaviour
{
    [SerializeField] private Transform hedef;

    public Transform Hedef
    {
        get => hedef;
        set => hedef = value;
    }

    private void Update()
    {
        bool clicked = false;
        Vector3 mousePosition = Vector3.zero;

        // 1. Support New Input System (active in Unity 6 PlayerSettings)
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
            Vector2 mPos = Mouse.current.position.ReadValue();
            mousePosition = new Vector3(mPos.x, mPos.y, 0f);
        }
#endif

        // 2. Support Legacy Input Manager / Both
        if (!clicked)
        {
            try
            {
                if (Input.GetMouseButtonDown(0))
                {
                    clicked = true;
                    mousePosition = Input.mousePosition;
                }
            }
            catch (System.InvalidOperationException)
            {
                // Thrown if active input handler is set to Input System Package only
            }
        }

        if (clicked)
        {
            MoveTargetToMouse(mousePosition);
        }
    }

    public bool MoveTargetToMouse(Vector3 mousePosition)
    {
        Camera cam = Camera.main != null ? Camera.main : GetComponent<Camera>();
        if (cam == null || hedef == null) return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            hitPoint.y = hedef.position.y;
            hedef.position = hitPoint;
            return true;
        }
        else
        {
            // If the ray points toward or above the horizon (ray.direction.y >= 0),
            // a forward mathematical plane raycast does not intersect Y = 0.
            // Project forward along the ray to the target's distance and place on ground XZ.
            float targetDistance = Mathf.Max(30f, Vector3.Distance(cam.transform.position, hedef.position));
            Vector3 projectedPoint = ray.origin + ray.direction * targetDistance;
            projectedPoint.y = hedef.position.y;
            hedef.position = projectedPoint;
            return true;
        }
    }

    // Retained for programmatic or test invocation
    public bool HandleMouseClick(Vector3? customMousePos = null)
    {
        Vector3 mPos = customMousePos.HasValue ? customMousePos.Value : GetMousePosition();
        return MoveTargetToMouse(mPos);
    }

    private Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            return new Vector3(pos.x, pos.y, 0f);
        }
#endif
        try
        {
            return Input.mousePosition;
        }
        catch (System.InvalidOperationException) { }

        return Vector3.zero;
    }

    public void SetTargetPosition(Vector3 newWorldPosition)
    {
        if (hedef != null)
        {
            newWorldPosition.y = hedef.position.y;
            hedef.position = newWorldPosition;
        }
    }
}

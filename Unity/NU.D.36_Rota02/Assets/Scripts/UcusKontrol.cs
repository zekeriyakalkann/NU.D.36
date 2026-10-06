using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public class UcusKontrol : MonoBehaviour
{
    [Header("Engine / Motor")]
    [Tooltip("Maximum engine thrust in Newtons (N)")]
    [SerializeField] private float maxThrust = 5000f;
    [Tooltip("Rate of throttle adjustment per second")]
    [SerializeField] private float throttleChangeRate = 0.5f;
    [Tooltip("Current throttle value (0.0 to 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float currentThrottle = 0f;

    [Header("Aerodynamics / Aerodinamik")]
    [Tooltip("Wing area in square meters (NU.D.36 reference: 21.8 m^2)")]
    [SerializeField] private float wingArea = 21.8f;
    [Tooltip("Air density in kg/m^3 (ISA sea-level: 1.225 kg/m^3)")]
    [SerializeField] private float airDensity = 1.225f;
    [Tooltip("Base lift coefficient")]
    [SerializeField] private float liftCoefficient = 0.55f;
    [Tooltip("Base aerodynamic drag coefficient")]
    [SerializeField] private float dragCoefficient = 0.035f;
    [Tooltip("Maximum safe airspeed in m/s")]
    [SerializeField] private float maxAirspeed = 55f;

    [Header("Flight Controls / Kumandalar")]
    [Tooltip("Pitch control authority (Up/Down Arrows)")]
    [SerializeField] private float pitchTorque = 1800f;
    [Tooltip("Roll control authority (A/D)")]
    [SerializeField] private float rollTorque = 2200f;
    [Tooltip("Yaw control authority (Q/E)")]
    [SerializeField] private float yawTorque = 1000f;
    [Tooltip("Additional lift coefficient per pitch input")]
    [SerializeField] private float pitchLiftBonus = 0.35f;

    [Header("Stability / Stabilite")]
    [Tooltip("Damping torque to resist rapid uncontrolled spinning")]
    [SerializeField] private float aerodynamicDamping = 1.8f;
    [Tooltip("Directional weather-vaning stability")]
    [SerializeField] private float directionalStability = 1.2f;

    [Header("Wheel Physics & Friction / Tekerlek Fizigi")]
    [Tooltip("PhysicMaterial for landing gear wheels (low friction to simulate rolling)")]
    [SerializeField] private PhysicsMaterial wheelPhysicMaterial;

    [Header("Camera Follow (Optional)")]
    [SerializeField] private Transform followCamera;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 3.5f, -10f);

    [Header("Diagnostics / Teshis")]
    [SerializeField] private bool showDebugHUD = true;

    private Rigidbody rb;
    private bool hasReportedThrottleStart = false;

    // Smoothed flight control inputs
    private float targetPitch = 0f;
    private float targetRoll = 0f;
    private float targetYaw = 0f;
    private float smoothPitch = 0f;
    private float smoothRoll = 0f;
    private float smoothYaw = 0f;

    public float CurrentThrottle => currentThrottle;
    public float Airspeed => rb != null ? Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, transform.forward)) : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SetupWheelPhysics();
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.mass = 900f;
            rb.linearDamping = 0f;
            rb.angularDamping = 2f;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Debug.Log($"[UcusKontrol] Initialized on {gameObject.name}. Mass: {rb.mass}kg, Gravity: {rb.useGravity}, Kinematic: {rb.isKinematic}, Constraints: {rb.constraints}, MaxThrust: {maxThrust}N.");
        }

        if (followCamera == null && Camera.main != null)
        {
            followCamera = Camera.main.transform;
        }
    }

    private float lastLogTime = 0f;

    private void SetupWheelPhysics()
    {
        if (wheelPhysicMaterial == null)
        {
            wheelPhysicMaterial = new PhysicsMaterial("WheelRollingPhysics")
            {
                dynamicFriction = 0.05f,
                staticFriction = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        var colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            if (col is SphereCollider)
            {
                col.material = wheelPhysicMaterial;
            }
        }

        var runway = GameObject.Find("Runway");
        if (runway != null)
        {
            var runwayCol = runway.GetComponent<Collider>();
            if (runwayCol != null)
            {
                runwayCol.material = wheelPhysicMaterial;
            }
        }
    }

    private void Update()
    {
        ReadInputs();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        ApplyThrust();
        ApplyLift();
        ApplyDrag();
        ApplyFlightControls();
        ApplyStability();

        if (currentThrottle > 0.05f && Time.fixedTime - lastLogTime >= 1.0f)
        {
            lastLogTime = Time.fixedTime;
            Debug.Log($"[UcusKontrol] Telemetry: Throttle={currentThrottle * 100f:F0}%, Thrust={currentThrottle * maxThrust:F0}N, Airspeed={Airspeed:F1}m/s ({Airspeed * 3.6f:F0}km/h), PosZ={transform.position.z:F2}m, VelY={rb.linearVelocity.y:F2}m/s");
        }
    }

    private void LateUpdate()
    {
        if (followCamera != null)
        {
            Vector3 targetCamPos = transform.position + transform.rotation * cameraOffset;
            followCamera.position = Vector3.Lerp(followCamera.position, targetCamPos, 12f * Time.deltaTime);
            Quaternion targetCamRot = Quaternion.LookRotation(transform.forward, transform.up);
            followCamera.rotation = Quaternion.Slerp(followCamera.rotation, targetCamRot, 12f * Time.deltaTime);
        }
    }

    private void ReadInputs()
    {
        bool wPressed = false;
        bool sPressed = false;
        float pitch = 0f;
        float roll = 0f;
        float yaw = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) wPressed = true;
            if (keyboard.sKey.isPressed) sPressed = true;
            if (keyboard.downArrowKey.isPressed) pitch += 1f;
            if (keyboard.upArrowKey.isPressed) pitch -= 1f;
            if (keyboard.aKey.isPressed) roll += 1f;
            if (keyboard.dKey.isPressed) roll -= 1f;
            if (keyboard.qKey.isPressed) yaw -= 1f;
            if (keyboard.eKey.isPressed) yaw += 1f;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKey(KeyCode.W)) wPressed = true;
            if (Input.GetKey(KeyCode.S)) sPressed = true;
            if (Input.GetKey(KeyCode.DownArrow)) pitch += 1f;
            if (Input.GetKey(KeyCode.UpArrow)) pitch -= 1f;
            if (Input.GetKey(KeyCode.A)) roll += 1f;
            if (Input.GetKey(KeyCode.D)) roll -= 1f;
            if (Input.GetKey(KeyCode.Q)) yaw -= 1f;
            if (Input.GetKey(KeyCode.E)) yaw += 1f;
        }
        catch (System.InvalidOperationException) { }
#endif

        // Throttle control: W = increase, S = decrease
        if (wPressed)
        {
            currentThrottle = Mathf.Clamp01(currentThrottle + throttleChangeRate * Time.deltaTime);
            if (!hasReportedThrottleStart && currentThrottle > 0.05f)
            {
                hasReportedThrottleStart = true;
                Debug.Log($"[UcusKontrol] W key active! Throttle: {currentThrottle:P0}, Thrust: {currentThrottle * maxThrust:F0}N, Airspeed: {Airspeed:F1}m/s.");
            }
        }
        else if (sPressed)
        {
            currentThrottle = Mathf.Clamp01(currentThrottle - throttleChangeRate * Time.deltaTime);
        }

        targetPitch = pitch;
        targetRoll = roll;
        targetYaw = yaw;

        // Smooth control response
        smoothPitch = Mathf.MoveTowards(smoothPitch, targetPitch, 4f * Time.deltaTime);
        smoothRoll = Mathf.MoveTowards(smoothRoll, targetRoll, 4f * Time.deltaTime);
        smoothYaw = Mathf.MoveTowards(smoothYaw, targetYaw, 4f * Time.deltaTime);
    }

    private void ApplyThrust()
    {
        // Forward engine thrust: T = throttle * maxThrust
        float thrust = currentThrottle * maxThrust;
        Vector3 thrustForce = transform.forward * thrust;
        rb.AddForce(thrustForce, ForceMode.Force);
    }

    private void ApplyLift()
    {
        // Forward airspeed along aircraft longitudinal axis
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, transform.forward));

        if (forwardSpeed <= 0.1f) return;

        // Effective lift coefficient modulated by pitch input (representing elevator angle of attack change)
        float effectiveCL = Mathf.Max(0f, liftCoefficient + (smoothPitch * pitchLiftBonus));

        // Simplified lift formula: L = 0.5 * rho * v^2 * S * C_L
        float liftMagnitude = 0.5f * airDensity * (forwardSpeed * forwardSpeed) * wingArea * effectiveCL;

        // Apply lift force upward relative to the aircraft's orientation
        Vector3 liftForce = transform.up * liftMagnitude;
        rb.AddForce(liftForce, ForceMode.Force);
    }

    private void ApplyDrag()
    {
        float totalSpeed = rb.linearVelocity.magnitude;
        if (totalSpeed <= 0.1f) return;

        Vector3 velocityDir = rb.linearVelocity.normalized;

        // Aerodynamic drag formula: D = 0.5 * rho * v^2 * S * C_D
        float dragMagnitude = 0.5f * airDensity * (totalSpeed * totalSpeed) * wingArea * dragCoefficient;

        // Gentle safety overspeed limiting: increase drag if exceeding max airspeed
        if (totalSpeed > maxAirspeed)
        {
            float excess = totalSpeed - maxAirspeed;
            dragMagnitude += excess * 150f;
        }

        Vector3 dragForce = -velocityDir * dragMagnitude;
        rb.AddForce(dragForce, ForceMode.Force);
    }

    private void ApplyFlightControls()
    {
        // Control surface effectiveness scales with airspeed and propwash airflow
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, transform.forward));
        float effectiveness = Mathf.Clamp01(0.25f + 0.75f * (forwardSpeed / 25f));

        // Pitch torque (around local right X-axis)
        Vector3 pitch = transform.right * (smoothPitch * pitchTorque * effectiveness);

        // Roll torque (around local forward Z-axis)
        Vector3 roll = transform.forward * (smoothRoll * rollTorque * effectiveness);

        // Yaw torque (around local up Y-axis)
        Vector3 yaw = transform.up * (smoothYaw * yawTorque * effectiveness);

        rb.AddTorque(pitch + roll + yaw, ForceMode.Force);
    }

    private void ApplyStability()
    {
        // Aerodynamic angular damping: resists fast spins and flutters
        Vector3 dampingTorque = -rb.angularVelocity * (aerodynamicDamping * 120f);
        rb.AddTorque(dampingTorque, ForceMode.Force);

        // Directional weather-vaning: restores alignment with flight direction
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (forwardSpeed > 3f)
        {
            Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
            // Rudder/fin restoring yaw
            Vector3 yawCorrection = transform.up * (localVel.x * directionalStability * 45f);
            // Horizontal tail restoring pitch
            Vector3 pitchCorrection = transform.right * (-localVel.y * directionalStability * 35f);

            rb.AddTorque(yawCorrection + pitchCorrection, ForceMode.Force);
        }
    }

    private void OnGUI()
    {
        if (!showDebugHUD || rb == null) return;

        GUI.Box(new Rect(10, 10, 240, 110), "NU.D.36 Flight Data");
        GUI.Label(new Rect(20, 32, 220, 20), $"Throttle: {currentThrottle * 100f:F0}%  (Thrust: {currentThrottle * maxThrust:F0} N)");
        GUI.Label(new Rect(20, 52, 220, 20), $"Airspeed: {Airspeed:F1} m/s ({Airspeed * 3.6f:F0} km/h)");
        GUI.Label(new Rect(20, 72, 220, 20), $"Altitude: {transform.position.y:F1} m");
        GUI.Label(new Rect(20, 92, 220, 20), $"Position Z: {transform.position.z:F1} m");
    }
}

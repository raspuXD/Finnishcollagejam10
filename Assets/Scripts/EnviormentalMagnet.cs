using UnityEngine;

public class EnvironmentalMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    public MagnetPolarity polarity  = MagnetPolarity.Attract;
    public float          force     = 80f;
    public float          range     = 15f;

    [Header("Affect Layers")]
    public bool affectMetalObjects = true;
    public bool affectPlayer       = true;
    public bool affectEnemies      = true;

    [Header("Player Control Reduction")]
    public float playerControlInRange = 0.3f;
    public float controlLoseSpeed     = 5f;
    public float controlRecoverSpeed  = 3f;

    [Header("Launch Override")]
    public float launchDisableTime = 0.25f;
    private float launchDisableUntil = 0f;

    [Header("Polarity Switch Launch")]
    public float launchForce         = 25f;   // burst force when player switches to matching polarity
    public float launchControlCutoff = 0.1f;  // control drops to this on launch
    public float launchCooldown      = 0.5f;  // prevent double firing
    private float lastLaunchTime     = -999f;

    [Header("Falloff")]
    public bool  useFalloff   = true;
    public float falloffPower = 2f;

    [Header("Pulse (optional)")]
    public bool  pulse                = false;
    public float pulseInterval        = 3f;
    public float pulseDuration        = 0.5f;
    public float pulseForceMultiplier = 3f;

    [Header("Visuals")]
    public Renderer targetRenderer;
    public Color    attractColor = new Color(0.2f, 0.5f, 1f);
    public Color    repelColor   = new Color(1f, 0.3f, 0.2f);

    private bool  isPulsing              = false;
    private float pulseTimer             = 0f;
    private float currentForceMultiplier = 1f;

    [SerializeField] private Rigidbody       playerRb;
    [SerializeField] private PlayerController playerController;
    private MagnetController playerMagnet;

    private float currentPlayerControl = 1f;
    private bool  playerInRange        = false;

    // Track last known polarity to detect changes
    private MagnetPolarity lastPlayerPolarity;
    private bool           lastMagnetEnabled;

    void Start()
    {
        if (targetRenderer != null)
            targetRenderer.material.color = polarity == MagnetPolarity.Attract
                                            ? attractColor : repelColor;
    }

    void Update()
    {
        if (playerRb == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            playerRb         = player.GetComponent<Rigidbody>();
            playerController = player.GetComponent<PlayerController>();
            playerMagnet     = player.GetComponent<MagnetController>();

            if (playerMagnet != null)
            {
                lastPlayerPolarity = playerMagnet.currentPolarity;
                lastMagnetEnabled  = playerMagnet.currentEnabled == MagnetController.Enabled.ON;
            }

            return;
        }
    }

    void CheckPolaritySwitch()
    {
        if (playerMagnet == null || playerRb == null) return;

        float distance = Vector3.Distance(transform.position, playerRb.position);
        if (distance > range) return;

        if (Time.time < lastLaunchTime + launchCooldown) return;

        bool magnetOn      = playerMagnet.currentEnabled == MagnetController.Enabled.ON;
        MagnetPolarity current = playerMagnet.currentPolarity;

        // Detect a polarity switch or magnet turn-on this frame
        bool polarityChanged = current != lastPlayerPolarity;
        bool justTurnedOn    = magnetOn && !lastMagnetEnabled;

        if ((polarityChanged || justTurnedOn) && magnetOn)
        {
            // Player's polarity now matches magnet = repel = launch them away
            if (current == polarity)
            {
                LaunchPlayer();
            }
        }

        lastPlayerPolarity = current;
        lastMagnetEnabled  = magnetOn;
    }

    void LaunchPlayer()
    {
        Vector3 dir = (playerRb.position - transform.position).normalized;

        playerRb.linearVelocity = Vector3.zero;
        playerRb.linearDamping  = 0f;  // kill drag so the launch carries
        playerRb.AddForce(dir * launchForce, ForceMode.Impulse);

        lastLaunchTime    = Time.time;
        launchDisableUntil = Time.time + launchDisableTime;
    }

    void FixedUpdate()
    {
        HandlePulse();
        ApplyMagnetism();
        HandlePlayerControl();
        CheckPolaritySwitch();
    }

    void HandlePlayerControl()
    {
        if (playerController == null) return;

        float target = playerInRange ? playerControlInRange : 1f;
        float speed  = playerInRange ? controlLoseSpeed : controlRecoverSpeed;

        currentPlayerControl = Mathf.MoveTowards(
            currentPlayerControl,
            target,
            speed * Time.fixedDeltaTime
        );

        if (currentPlayerControl < playerController.controlMultiplier)
            playerController.controlMultiplier = currentPlayerControl;

        playerInRange = false;
    }

    void HandlePulse()
    {
        if (!pulse) return;

        pulseTimer += Time.fixedDeltaTime;

        if (!isPulsing && pulseTimer >= pulseInterval)
        {
            isPulsing              = true;
            pulseTimer             = 0f;
            currentForceMultiplier = pulseForceMultiplier;
        }

        if (isPulsing)
        {
            pulseTimer += Time.fixedDeltaTime;
            if (isPulsing && pulseTimer >= pulseDuration)
            {
                isPulsing              = false;
                pulseTimer             = 0f;
                currentForceMultiplier = 1f;
            }
        }
    }

    void ApplyMagnetism()
    {
        if (Time.time < launchDisableUntil)
        return;
        Collider[] hits = Physics.OverlapSphere(transform.position, range);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            Rigidbody targetRb    = null;
            bool      shouldAffect = false;

            MetalObject metal = hit.GetComponent<MetalObject>();
            if (metal != null && metal.rb != null)
            {
                bool isEnemy = metal.objectType == MetalObject.ObjectType.Enemy;
                if (isEnemy  && affectEnemies)     { targetRb = metal.rb; shouldAffect = true; }
                if (!isEnemy && affectMetalObjects) { targetRb = metal.rb; shouldAffect = true; }
            }

            if (!shouldAffect && affectPlayer && playerRb != null
                && hit.gameObject == playerRb.gameObject)
            {
                targetRb      = playerRb;
                shouldAffect  = true;
                playerInRange = true;
            }

            if (!shouldAffect || targetRb == null) continue;

            Vector3 toTarget = hit.transform.position - transform.position;
            float   distance = toTarget.magnitude;
            if (distance < 0.05f) continue;

            Vector3 dir = toTarget.normalized;

            // If player's magnet is on and matches this magnet's polarity — repel
            // If player's magnet is on and opposite — attract
            // If player's magnet is off — use this magnet's own polarity setting
            bool repelPlayer = false;
            if (targetRb == playerRb && playerMagnet != null
                && playerMagnet.currentEnabled == MagnetController.Enabled.ON)
            {
                repelPlayer = playerMagnet.currentPolarity == polarity;
            }
            else
            {
                repelPlayer = polarity == MagnetPolarity.Repel;
            }

            Vector3 forceDir = repelPlayer ? dir : -dir;

            float falloff = 1f;
            if (useFalloff)
            {
                float t = 1f - Mathf.Clamp01(distance / range);
                falloff = Mathf.Pow(t, falloffPower);
            }

            targetRb.AddForce(forceDir * force * falloff * currentForceMultiplier,
                              ForceMode.Acceleration);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = polarity == MagnetPolarity.Attract
                       ? new Color(0.2f, 0.5f, 1f, 0.3f)
                       : new Color(1f, 0.3f, 0.2f, 0.3f);
        Gizmos.DrawSphere(transform.position, range);

        Gizmos.color = polarity == MagnetPolarity.Attract
                       ? new Color(0.2f, 0.5f, 1f)
                       : new Color(1f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
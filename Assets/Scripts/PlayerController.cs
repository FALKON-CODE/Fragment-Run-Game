using UnityEngine;

/// <summary>
/// Drives the player: responsive, non-floaty jumping (variable height, coyote
/// time and jump buffering), smooth accelerated running and a hand-animated
/// sprite rig (idle / two-frame walk cycle / jump) with facing flip and
/// squash-and-stretch juice.
///
/// The rig builds itself: a child "Body" object holds the SpriteRenderer and is
/// scaled <b>in code</b> from each frame's <c>sprite.bounds</c> so the character
/// always renders at a consistent height with its feet planted, no matter what
/// import settings Unity gives the textures. The player object is spawned at
/// runtime by <see cref="LevelManager"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Running")]
    public float moveSpeed = 7.5f;
    public float groundAccel = 90f;
    public float airAccel = 45f;
    public float groundFriction = 70f;

    [Header("Jump feel")]
    [Tooltip("How high a full jump reaches, in world units.")]
    public float jumpHeight = 3.2f;
    [Tooltip("Time to reach the top of a full jump. Smaller = snappier.")]
    public float timeToApex = 0.38f;
    [Tooltip("Gravity is multiplied by this while falling for a punchy, non-floaty descent.")]
    public float fallMultiplier = 1.7f;
    [Tooltip("Extra gravity while rising if the jump button is released (variable jump height).")]
    public float lowJumpMultiplier = 2.4f;
    [Tooltip("Grace period after leaving a ledge where you can still jump.")]
    public float coyoteTime = 0.10f;
    [Tooltip("A jump pressed this long before landing still fires on touchdown.")]
    public float jumpBuffer = 0.12f;

    [Header("Bounds")]
    [Tooltip("Falling below this Y kills the player and restarts the level.")]
    public float killY = -12f;

    // Visual size of the character in world units (feet on the ground, head at top).
    private const float CharacterHeight = 1.75f;

    private Rigidbody2D rb;
    private CapsuleCollider2D body;
    private Transform rig;            // child that carries the sprite
    private SpriteRenderer rigSr;

    private Sprite sprIdle, sprWalk1, sprWalk2, sprJump;

    private float moveInput;
    private bool grounded;
    private bool wasGrounded;
    private float coyoteCounter;
    private float bufferCounter;
    private float baseGravityScale;
    private int facing = 1;

    // Animation state.
    private float stepTimer;
    private int walkFrame;
    private float breathe;
    private Vector2 squash = Vector2.one;   // (x,y) multipliers, eased back to 1

    private bool CanControl => GameManager.Instance == null || GameManager.Instance.IsPlaying;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Derive gravity and jump velocity from the designer-friendly
        // "height + time to apex" so tuning stays intuitive and snappy.
        float gravity = (2f * jumpHeight) / (timeToApex * timeToApex);
        baseGravityScale = gravity / Mathf.Abs(Physics2D.gravity.y);
        rb.gravityScale = baseGravityScale;

        // A capsule body slides over ledges and seams better than a box.
        body = gameObject.AddComponent<CapsuleCollider2D>();
        body.direction = CapsuleDirection2D.Vertical;
        body.size = new Vector2(0.62f, 1.6f);
        body.offset = new Vector2(0f, 0.8f);

        BuildRig();
    }

    // ----- Rig construction ---------------------------------------------

    void BuildRig()
    {
        sprIdle = LoadSprite("Art/player_idle");
        sprWalk1 = LoadSprite("Art/player_walk1");
        sprWalk2 = LoadSprite("Art/player_walk2");
        sprJump = LoadSprite("Art/player_jump");

        GameObject go = new GameObject("Body");
        go.transform.SetParent(transform, false);
        rig = go.transform;

        rigSr = go.AddComponent<SpriteRenderer>();
        rigSr.sortingOrder = 10;

        if (sprIdle != null)
        {
            ApplyFrame(sprIdle);
            ApplyRigTransform(0f, 1f);   // size correctly before the first frame renders
        }
        else
        {
            // Fallback so the game is still playable if the art is missing.
            rigSr.sprite = Resources.Load<Sprite>("Sprites/white");
            rigSr.color = new Color(0.20f, 0.90f, 1.00f);
            rig.localScale = new Vector3(0.7f, CharacterHeight, 1f);
            rig.localPosition = new Vector3(0f, CharacterHeight * 0.5f, 0f);
        }
    }

    /// <summary>
    /// Loads a character frame robustly. If Unity imported the PNG as a proper
    /// Sprite we use it directly; if it reset the texture to "Default" type
    /// (which drops the Sprite), we rebuild the Sprite from the raw texture in
    /// code with a centred pivot. Either way the character always appears.
    /// </summary>
    Sprite LoadSprite(string path)
    {
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;

        Texture2D tex = Resources.Load<Texture2D>(path);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Swaps to a frame and rescales it so every pose renders at the same height
    /// with the feet planted at the object origin. Uses the sprite's world
    /// bounds, so it is immune to whatever pixels-per-unit Unity imported.
    /// </summary>
    void ApplyFrame(Sprite s)
    {
        if (s == null || rigSr.sprite == s) return;
        rigSr.sprite = s;
        rigSr.color = Color.white;
    }

    // ----- Input ---------------------------------------------------------

    void Update()
    {
        if (!CanControl)
        {
            moveInput = 0f;
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
            bufferCounter = jumpBuffer;

        // Release the button early to cut the jump short (variable height).
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        if (transform.position.y < killY && GameManager.Instance != null)
            GameManager.Instance.PlayerDied();

        Animate();
    }

    // ----- Physics -------------------------------------------------------

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        grounded = GroundCheck();

        // Landing: squash and kick up a little dust.
        if (grounded && !wasGrounded && rb.linearVelocity.y <= 0.5f)
        {
            squash = new Vector2(1.25f, 0.75f);
            Effects.SpawnDust(transform.position, 5, 0.7f);
        }
        wasGrounded = grounded;

        // Coyote & buffer timers.
        coyoteCounter = grounded ? coyoteTime : coyoteCounter - dt;
        bufferCounter -= dt;

        // Horizontal movement with acceleration / friction for weight.
        float target = moveInput * moveSpeed;
        float rate = Mathf.Abs(moveInput) > 0.01f
            ? (grounded ? groundAccel : airAccel)
            : (grounded ? groundFriction : airAccel * 0.5f);
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, rate * dt);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        // Jump: fires from buffered press while grounded or within coyote time.
        if (bufferCounter > 0f && coyoteCounter > 0f)
        {
            float jumpVelocity = (2f * jumpHeight) / timeToApex;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            bufferCounter = 0f;
            coyoteCounter = 0f;
            grounded = false;
            squash = new Vector2(0.75f, 1.28f);   // stretch on launch
            Effects.SpawnDust(transform.position, 4, 0.6f);
        }

        // Gravity shaping: fall faster than you rise, and fall fast if the
        // jump was released early. This is what removes the "floaty" feel.
        float g = baseGravityScale;
        if (rb.linearVelocity.y < -0.01f)
            g *= fallMultiplier;
        else if (rb.linearVelocity.y > 0.01f && !Input.GetButton("Jump"))
            g *= lowJumpMultiplier;
        rb.gravityScale = g;

        // Track facing from movement intent.
        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;
    }

    bool GroundCheck()
    {
        // A thin box just under the feet; ignore ourselves and any triggers.
        Vector2 center = (Vector2)transform.position + new Vector2(0f, 0.06f);
        Vector2 size = new Vector2(body.size.x * 0.9f, 0.14f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D h in hits)
        {
            if (h == null || h.isTrigger) continue;
            if (h.attachedRigidbody == rb) continue;
            return true;
        }
        return false;
    }

    // ----- Animation -----------------------------------------------------

    void Animate()
    {
        float dt = Time.deltaTime;
        float speed01 = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / moveSpeed);

        if (!grounded)
        {
            ApplyFrame(sprJump);
            stepTimer = 0f;
        }
        else if (speed01 > 0.12f)
        {
            // Faster running = faster steps, so the walk cycle reads naturally.
            float interval = Mathf.Lerp(0.26f, 0.11f, speed01);
            stepTimer += dt;
            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                walkFrame ^= 1;
            }
            ApplyFrame(walkFrame == 0 ? sprWalk1 : sprWalk2);
        }
        else
        {
            ApplyFrame(sprIdle);
            stepTimer = 0f;
        }

        // Ease squash/stretch back to neutral.
        squash = Vector2.MoveTowards(squash, Vector2.one, 6f * dt);

        // Subtle idle breathing and a tiny walk bob for life.
        breathe += dt * (speed01 > 0.12f ? 12f : 2.5f);
        float bobY = grounded ? Mathf.Abs(Mathf.Sin(breathe)) * (speed01 > 0.12f ? 0.05f : 0.015f) : 0f;
        float breatheScale = grounded && speed01 <= 0.12f ? 1f + Mathf.Sin(breathe) * 0.015f : 1f;

        ApplyRigTransform(bobY, breatheScale);
    }

    /// <summary>
    /// Composes facing, size-normalisation, squash/stretch and bob into the
    /// child transform, keeping the feet locked to the object origin.
    /// </summary>
    void ApplyRigTransform(float bobY, float breatheScale)
    {
        if (rig == null || rigSr.sprite == null) return;

        float spriteWorldHeight = rigSr.sprite.bounds.size.y;
        if (spriteWorldHeight <= 0.0001f) return;

        // Base uniform scale that makes this frame CharacterHeight tall.
        float baseScale = CharacterHeight / spriteWorldHeight;

        float sx = baseScale * squash.x * facing;
        float sy = baseScale * squash.y * breatheScale;
        rig.localScale = new Vector3(sx, sy, 1f);

        // Height after scaling; place centre-pivoted sprite so its feet sit at y=0.
        float renderedHeight = CharacterHeight * squash.y * breatheScale;
        rig.localPosition = new Vector3(0f, renderedHeight * 0.5f + bobY, 0f);
    }

    // ----- World interactions -------------------------------------------

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null) return;

        if (other.CompareTag("Finish") || other.gameObject.name == "Exit")
            GameManager.Instance.LevelComplete();
        else if (other.gameObject.name == "Hazard")
            GameManager.Instance.PlayerDied();
    }
}

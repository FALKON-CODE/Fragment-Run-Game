using UnityEngine;

/// <summary>
/// Handles player horizontal movement, jumping, a simple camera follow and
/// reacting to the level's exit / falling out of the world.
/// The player object is created at runtime by <see cref="LevelManager"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Bounds")]
    [Tooltip("Falling below this Y kills the player and restarts the level.")]
    public float killY = -12f;

    private Rigidbody2D rb;
    private float moveInput;
    private bool grounded;
    private bool jumpQueued;

    private bool CanControl => GameManager.Instance == null || GameManager.Instance.IsPlaying;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!CanControl)
        {
            moveInput = 0f;
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && grounded)
            jumpQueued = true;

        // Died by falling out of the world.
        if (transform.position.y < killY && GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
    }

    void FixedUpdate()
    {
        // Keep vertical velocity, drive horizontal from input.
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (jumpQueued)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpQueued = false;
            grounded = false;
        }
    }

    // Simple ground check: we are grounded when standing on top of a collider.
    void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                grounded = true;
                return;
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        grounded = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.Instance == null) return;

        if (other.CompareTag("Finish") || other.gameObject.name == "Exit")
            GameManager.Instance.LevelComplete();
        else if (other.gameObject.name == "Hazard")
            GameManager.Instance.PlayerDied();
    }

    void LateUpdate()
    {
        // Simple camera follow on the X axis.
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            camPos.x = transform.position.x;
            cam.transform.position = camPos;
        }
    }
}

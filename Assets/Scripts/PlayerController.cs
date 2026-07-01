using UnityEngine;

/// <summary>
/// Handles player horizontal movement, jumping and a simple camera follow.
/// The player object is created at runtime by <see cref="LevelManager"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    private Rigidbody2D rb;
    private float moveInput;
    private bool grounded;
    private bool jumpQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && grounded)
            jumpQueued = true;
    }

    void FixedUpdate()
    {
        // Keep vertical velocity, drive horizontal from input.
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        if (jumpQueued)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
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

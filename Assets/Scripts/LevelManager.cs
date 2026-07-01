using UnityEngine;

/// <summary>
/// Builds a level from code so the scene files stay small and readable.
/// The level to build is chosen by <see cref="levelNumber"/>, set per scene.
/// It also makes sure a <see cref="GameManager"/> exists for the scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level")]
    public int levelNumber = 1;

    // Shared colours so every level has a consistent look.
    private static readonly Color GroundColor = new Color(0.15f, 0.18f, 0.28f);
    private static readonly Color PlatformColor = new Color(0.22f, 0.26f, 0.38f);
    private static readonly Color PlayerColor = new Color(0.20f, 0.90f, 1.00f);
    private static readonly Color ExitColor = new Color(0.35f, 1.00f, 0.55f);

    private Sprite squareSprite;

    void Start()
    {
        squareSprite = Resources.Load<Sprite>("Sprites/white");
        EnsureGameManager();
        BuildLevel();
    }

    void EnsureGameManager()
    {
        if (GameManager.Instance == null)
            gameObject.AddComponent<GameManager>();

        GameManager.Instance.CurrentLevel = levelNumber;
    }

    void BuildLevel()
    {
        switch (levelNumber)
        {
            case 1:
                BuildLevel1();
                break;
            default:
                BuildLevel1();
                break;
        }
    }

    // ----- Level layouts -------------------------------------------------

    void BuildLevel1()
    {
        // Two ground segments with a gap in the middle to jump over.
        CreateBlock("Ground", new Vector2(-9f, -4f), new Vector2(18f, 1f), GroundColor);
        CreateBlock("Ground", new Vector2(11f, -4f), new Vector2(14f, 1f), GroundColor);

        // The exit door sits at the far right, on top of the second segment.
        CreateExit(new Vector2(16f, -2.3f), new Vector2(1.4f, 2.4f));

        SpawnPlayer(new Vector2(-14f, -2f));
    }

    // ----- Building blocks ----------------------------------------------

    /// <summary>Creates a solid, collidable block sized in world units.</summary>
    GameObject CreateBlock(string blockName, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(blockName);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = color;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        return go;
    }

    /// <summary>Creates the glowing exit door (a trigger the player walks into).</summary>
    GameObject CreateExit(Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject("Exit");
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = ExitColor;
        sr.sortingOrder = 2;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        box.isTrigger = true;
        return go;
    }

    GameObject SpawnPlayer(Vector2 position)
    {
        GameObject go = new GameObject("Player");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 1.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = PlayerColor;
        sr.sortingOrder = 5;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;

        go.AddComponent<PlayerController>();
        return go;
    }
}

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
    private static readonly Color HazardColor = new Color(1.00f, 0.30f, 0.35f);

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

        // GameManager builds the UI in its Awake, so it exists by now.
        if (UIManager.Instance != null)
            UIManager.Instance.SetLevel(levelNumber);
    }

    void BuildLevel()
    {
        switch (levelNumber)
        {
            case 2:
                BuildLevel2();
                break;
            case 3:
                BuildLevel3();
                break;
            default:
                BuildLevel1();
                break;
        }
    }

    // ----- Level layouts -------------------------------------------------

    // Level 1: gentle introduction – a single gap to jump over.
    void BuildLevel1()
    {
        CreateBlock("Ground", new Vector2(-9f, -4f), new Vector2(18f, 1f), GroundColor);
        CreateBlock("Ground", new Vector2(11f, -4f), new Vector2(14f, 1f), GroundColor);

        CreateExit(new Vector2(16f, -2.3f), new Vector2(1.4f, 2.4f));

        SpawnPlayer(new Vector2(-14f, -2f));
    }

    // Level 2: more gaps, a floating platform and the first hazard.
    void BuildLevel2()
    {
        CreateBlock("Ground", new Vector2(-16f, -4f), new Vector2(8f, 1f), GroundColor);
        CreateBlock("Ground", new Vector2(-6f, -4f), new Vector2(6f, 1f), GroundColor);

        // A spike sitting on the second ground segment: jump over it.
        CreateHazard(new Vector2(-6f, -3.15f), new Vector2(1f, 0.7f));

        CreatePlatform(new Vector2(3f, -2.5f), new Vector2(4f, 0.5f));

        CreateBlock("Ground", new Vector2(12f, -4f), new Vector2(10f, 1f), GroundColor);

        CreateExit(new Vector2(15f, -2.3f), new Vector2(1.4f, 2.4f));

        SpawnPlayer(new Vector2(-18f, -2f));
    }

    // Level 3: a run of narrow platforms and a guarded exit – the toughest level.
    void BuildLevel3()
    {
        CreateBlock("Ground", new Vector2(-17f, -4f), new Vector2(6f, 1f), GroundColor);

        CreatePlatform(new Vector2(-9f, -3f), new Vector2(3f, 0.5f));
        CreatePlatform(new Vector2(-2f, -2f), new Vector2(3f, 0.5f));
        CreatePlatform(new Vector2(5f, -3f), new Vector2(3f, 0.5f));

        CreateBlock("Ground", new Vector2(13f, -4f), new Vector2(10f, 1f), GroundColor);

        // A spike guarding the run up to the exit.
        CreateHazard(new Vector2(11f, -3.15f), new Vector2(1f, 0.7f));

        CreateExit(new Vector2(16f, -2.3f), new Vector2(1.4f, 2.4f));

        SpawnPlayer(new Vector2(-18f, -2f));
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

    /// <summary>A smaller floating platform the player can land on.</summary>
    GameObject CreatePlatform(Vector2 position, Vector2 size)
    {
        return CreateBlock("Platform", position, size, PlatformColor);
    }

    /// <summary>Creates a hazard (spike) that kills the player on touch.</summary>
    GameObject CreateHazard(Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject("Hazard");
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = HazardColor;
        sr.sortingOrder = 3;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;
        box.isTrigger = true;
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

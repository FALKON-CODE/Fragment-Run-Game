using UnityEngine;

/// <summary>
/// Builds a level from code so the scenes stay simple. The level to build is
/// chosen by <see cref="levelNumber"/>, which is set per scene in the Inspector.
/// Phase 1 only creates the ground and spawns the player.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level")]
    public int levelNumber = 1;

    private Sprite squareSprite;

    void Start()
    {
        squareSprite = Resources.Load<Sprite>("Sprites/white");
        BuildLevel();
    }

    void BuildLevel()
    {
        // Long ground platform for the player to run on.
        CreateBlock("Ground", new Vector2(0f, -4f), new Vector2(40f, 1f),
            new Color(0.15f, 0.18f, 0.28f));

        SpawnPlayer(new Vector2(-14f, -2f));
    }

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

    GameObject SpawnPlayer(Vector2 position)
    {
        GameObject go = new GameObject("Player");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 1.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.2f, 0.9f, 1f);
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

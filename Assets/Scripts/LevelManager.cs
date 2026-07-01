using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a level from code so the scene files stay small and readable.
/// The level to build is chosen by <see cref="levelNumber"/>, set per scene.
/// It also makes sure a <see cref="GameManager"/> exists for the scene and
/// wires up a smooth follow camera.
///
/// Everything gets a neon look: platforms glow, the exit is a pulsing portal,
/// hazards are spinning saw-blades and there are collectible energy fragments.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level")]
    public int levelNumber = 1;

    // Shared palette — a cohesive cyber-neon look across every level.
    private static readonly Color GroundColor = new Color(0.10f, 0.13f, 0.22f);
    private static readonly Color PlatformColor = new Color(0.16f, 0.20f, 0.32f);
    private static readonly Color NeonCyan = new Color(0.25f, 0.95f, 1.00f);
    private static readonly Color NeonMagenta = new Color(1.00f, 0.30f, 0.85f);
    private static readonly Color ExitColor = new Color(0.40f, 1.00f, 0.65f);
    private static readonly Color HazardColor = new Color(1.00f, 0.28f, 0.34f);
    private static readonly Color FragmentColor = new Color(1.00f, 0.86f, 0.30f);

    private Sprite squareSprite;
    private Sprite glowSprite;
    private int fragmentCount;

    void Start()
    {
        squareSprite = Resources.Load<Sprite>("Sprites/white");
        glowSprite = ProcArt.Glow();   // built in code: a true soft radial glow
        EnsureGameManager();
        BuildLevel();

        // Tell the HUD how many fragments this level holds.
        if (GameManager.Instance != null)
            GameManager.Instance.SetLevelFragments(fragmentCount);
    }

    void EnsureGameManager()
    {
        if (GameManager.Instance == null)
            gameObject.AddComponent<GameManager>();

        GameManager.Instance.CurrentLevel = levelNumber;

        if (UIManager.Instance != null)
            UIManager.Instance.SetLevel(levelNumber);
    }

    void BuildLevel()
    {
        CreateStarfield();

        switch (levelNumber)
        {
            case 2: BuildLevel2(); break;
            case 3: BuildLevel3(); break;
            default: BuildLevel1(); break;
        }
    }

    // ----- Level layouts -------------------------------------------------

    // Level 1: gentle introduction – a single gap to jump over.
    void BuildLevel1()
    {
        CreateBackground("Art/bg_level1");
        CreateBlock("Ground", new Vector2(-9f, -4f), new Vector2(18f, 1f), GroundColor, NeonCyan);
        CreateBlock("Ground", new Vector2(11f, -4f), new Vector2(14f, 1f), GroundColor, NeonCyan);

        CreateFragment(new Vector2(-6f, -2.6f));
        CreateFragment(new Vector2(1f, -1.4f));   // reward for jumping the gap
        CreateFragment(new Vector2(8f, -2.6f));

        CreateExit(new Vector2(16f, -2.3f));
        SpawnPlayer(new Vector2(-14f, -2f));
    }

    // Level 2: more gaps, a floating platform and the first hazard.
    void BuildLevel2()
    {
        CreateBackground("Art/bg_level2");
        CreateBlock("Ground", new Vector2(-16f, -4f), new Vector2(8f, 1f), GroundColor, NeonMagenta);
        CreateBlock("Ground", new Vector2(-6f, -4f), new Vector2(6f, 1f), GroundColor, NeonMagenta);

        CreateHazard(new Vector2(-6f, -3.0f));    // spinning blade to jump over

        CreatePlatform(new Vector2(3f, -2.5f), new Vector2(4f, 0.5f));
        CreateFragment(new Vector2(3f, -1.4f));

        CreateBlock("Ground", new Vector2(12f, -4f), new Vector2(10f, 1f), GroundColor, NeonMagenta);

        CreateFragment(new Vector2(-12f, -2.6f));
        CreateFragment(new Vector2(-2f, -1.6f));
        CreateFragment(new Vector2(11f, -2.6f));

        CreateExit(new Vector2(15f, -2.3f));
        SpawnPlayer(new Vector2(-18f, -2f));
    }

    // Level 3: a run of narrow platforms and a guarded exit – the toughest level.
    void BuildLevel3()
    {
        CreateBackground("Art/bg_level3");
        CreateBlock("Ground", new Vector2(-17f, -4f), new Vector2(6f, 1f), GroundColor, NeonCyan);

        CreatePlatform(new Vector2(-9f, -3f), new Vector2(3f, 0.5f));
        CreatePlatform(new Vector2(-2f, -2f), new Vector2(3f, 0.5f));
        CreatePlatform(new Vector2(5f, -3f), new Vector2(3f, 0.5f));

        CreateFragment(new Vector2(-9f, -1.9f));
        CreateFragment(new Vector2(-2f, -0.9f));
        CreateFragment(new Vector2(5f, -1.9f));

        CreateBlock("Ground", new Vector2(13f, -4f), new Vector2(10f, 1f), GroundColor, NeonCyan);

        CreateHazard(new Vector2(11f, -3.0f));    // blade guarding the exit run

        CreateFragment(new Vector2(15f, -2.6f));

        CreateExit(new Vector2(16f, -2.3f));
        SpawnPlayer(new Vector2(-18f, -2f));
    }

    // ----- Background & atmosphere --------------------------------------

    void CreateBackground(string resourcePath)
    {
        Sprite bg = Resources.Load<Sprite>(resourcePath);
        if (bg == null) return;

        Camera cam = Camera.main;
        GameObject go = new GameObject("Background");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bg;
        sr.color = new Color(0.45f, 0.52f, 0.66f, 1f);
        sr.sortingOrder = -100;

        if (cam != null)
        {
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 10f);

            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;
            Vector2 spriteSize = bg.bounds.size;
            // Oversize a touch so parallax drift never reveals an edge.
            go.transform.localScale = new Vector3(
                worldWidth / spriteSize.x * 1.15f, worldHeight / spriteSize.y * 1.15f, 1f);
        }
    }

    /// <summary>A drifting field of faint glowing motes for depth and mood.</summary>
    void CreateStarfield()
    {
        GameObject container = new GameObject("Starfield");
        Parallax par = container.AddComponent<Parallax>();
        par.factor = 0.35f;   // moves slower than the camera → feels distant

        for (int i = 0; i < 46; i++)
        {
            float x = Random.Range(-24f, 24f);
            float y = Random.Range(-5f, 6f);
            float size = Random.Range(0.12f, 0.45f);
            Color c = Random.value > 0.5f ? NeonCyan : NeonMagenta;
            c.a = Random.Range(0.10f, 0.35f);

            SpriteRenderer sr = MakeGlow(container.transform,
                new Vector3(x, y, 0f), size, c, -60);
            // A slow twinkle.
            Pulser p = sr.gameObject.AddComponent<Pulser>();
            p.speed = Random.Range(0.5f, 1.8f);
            p.minAlpha = c.a * 0.3f;
            p.maxAlpha = c.a;
        }
    }

    // ----- Building blocks ----------------------------------------------

    /// <summary>A solid block with a soft neon underglow and a bright top edge.</summary>
    GameObject CreateBlock(string blockName, Vector2 position, Vector2 size, Color fill, Color neon)
    {
        GameObject go = new GameObject(blockName);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = fill;
        sr.sortingOrder = 0;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = Vector2.one;

        // Bright neon strip along the top edge (scaled in local space).
        GameObject edge = new GameObject("Edge");
        edge.transform.SetParent(go.transform, false);
        edge.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        edge.transform.localScale = new Vector3(1f, 0.06f / size.y, 1f);
        SpriteRenderer esr = edge.AddComponent<SpriteRenderer>();
        esr.sprite = squareSprite;
        esr.color = neon;
        esr.sortingOrder = 1;

        // Soft glow bleeding up off that edge.
        MakeGlow(go.transform, new Vector3(0f, 0.5f, 0f),
            1f, new Color(neon.r, neon.g, neon.b, 0.25f), -1,
            new Vector2(size.x * 1.0f, 0.9f));

        return go;
    }

    GameObject CreatePlatform(Vector2 position, Vector2 size)
    {
        return CreateBlock("Platform", position, size, PlatformColor, NeonCyan);
    }

    /// <summary>A row of glowing spikes that kills the player on touch.</summary>
    GameObject CreateHazard(Vector2 position)
    {
        GameObject go = new GameObject("Hazard");
        go.transform.position = position;

        Sprite spike = TriangleSprite();
        const int count = 3;
        const float step = 0.62f;
        float baseY = -0.5f;   // spike bases sit on the ground below the trigger

        // Menacing red glow washing up from the spikes.
        Pulser hp = go.AddComponent<Pulser>();
        hp.target = MakeGlow(go.transform, new Vector3(0f, baseY + 0.3f, 0f), 2.4f,
            new Color(HazardColor.r, HazardColor.g, HazardColor.b, 0.4f), 1);
        hp.speed = 5f; hp.minAlpha = 0.2f; hp.maxAlpha = 0.5f; hp.scalePulse = 0.15f;

        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * 0.5f) * step;
            GameObject s = new GameObject("Spike");
            s.transform.SetParent(go.transform, false);
            s.transform.localPosition = new Vector3(x, baseY, 0f);
            s.transform.localScale = new Vector3(0.6f, 0.95f, 1f);
            SpriteRenderer ssr = s.AddComponent<SpriteRenderer>();
            ssr.sprite = spike;
            ssr.color = HazardColor;
            ssr.sortingOrder = 3;

            // Bright hot tip.
            SpriteRenderer tip = MakeGlow(s.transform, new Vector3(0f, 0.9f, 0f),
                0.5f, new Color(1f, 0.8f, 0.75f, 0.9f), 4);
            tip.transform.localScale *= 0.6f;
        }

        // Trigger box spanning the spikes.
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(count * step + 0.2f, 0.9f);
        box.offset = new Vector2(0f, baseY + 0.4f);
        box.isTrigger = true;
        return go;
    }

    // Cached procedural triangle so spikes/blades are real shapes, not squares.
    private static Sprite triangleSprite;
    private static Sprite TriangleSprite()
    {
        if (triangleSprite != null) return triangleSprite;
        const int n = 64;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        {
            float fy = y / (float)(n - 1);
            float halfW = (1f - fy) * 0.5f;   // widest at the base, a point at the top
            for (int x = 0; x < n; x++)
            {
                float fx = x / (float)(n - 1);
                bool inside = Mathf.Abs(fx - 0.5f) <= halfW;
                t.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        t.Apply();
        // Pivot at the bottom-centre so a spike sits on the ground.
        triangleSprite = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0f), 100f);
        return triangleSprite;
    }

    /// <summary>The glowing exit portal (a trigger the player walks into).</summary>
    GameObject CreateExit(Vector2 position)
    {
        GameObject go = new GameObject("Exit");
        go.transform.position = position;

        // A tall oval halo and a brighter inner glow read as a portal, not a box.
        MakeGlow(go.transform, Vector3.zero, 1f,
            new Color(ExitColor.r, ExitColor.g, ExitColor.b, 0.40f), 1, new Vector2(2.6f, 3.4f));
        MakeGlow(go.transform, Vector3.zero, 1f,
            new Color(ExitColor.r, ExitColor.g, ExitColor.b, 0.85f), 2, new Vector2(1.4f, 2.7f));

        // Bright pulsing energy core down the middle.
        SpriteRenderer core = MakeGlow(go.transform, Vector3.zero, 1f,
            Color.white, 4, new Vector2(0.8f, 2.1f));
        Pulser pulse = go.AddComponent<Pulser>();
        pulse.target = core;
        pulse.speed = 3f; pulse.minAlpha = 0.65f; pulse.maxAlpha = 1f; pulse.scalePulse = 0.12f;

        // Two thin neon pillars frame the doorway.
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject pillar = new GameObject("Pillar");
            pillar.transform.SetParent(go.transform, false);
            pillar.transform.localPosition = new Vector3(i * 0.62f, 0f, 0f);
            pillar.transform.localScale = new Vector3(0.12f, 2.6f, 1f);
            SpriteRenderer psr = pillar.AddComponent<SpriteRenderer>();
            psr.sprite = squareSprite;
            psr.color = ExitColor;
            psr.sortingOrder = 3;
        }

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.4f, 2.4f);
        box.isTrigger = true;
        return go;
    }

    /// <summary>A collectible energy fragment: spins, bobs, glows, adds to score.</summary>
    void CreateFragment(Vector2 position)
    {
        fragmentCount++;

        GameObject go = new GameObject("Fragment");
        go.transform.position = position;

        // Glow halo.
        Pulser halo = go.AddComponent<Pulser>();
        halo.target = MakeGlow(go.transform, Vector3.zero, 1.1f,
            new Color(FragmentColor.r, FragmentColor.g, FragmentColor.b, 0.5f), 4);
        halo.speed = 3.5f; halo.minAlpha = 0.3f; halo.maxAlpha = 0.6f; halo.scalePulse = 0.2f;

        // The spinning diamond core (a rotated square).
        GameObject core = new GameObject("Core");
        core.transform.SetParent(go.transform, false);
        core.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
        core.transform.localRotation = Quaternion.Euler(0, 0, 45f);
        SpriteRenderer csr = core.AddComponent<SpriteRenderer>();
        csr.sprite = squareSprite;
        csr.color = FragmentColor;
        csr.sortingOrder = 6;

        go.AddComponent<Spinner>().degreesPerSecond = 120f;   // spins the whole fragment
        go.AddComponent<Bobber>();

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.45f;
        col.isTrigger = true;

        go.AddComponent<Collectible>().glowColor = FragmentColor;
    }

    GameObject SpawnPlayer(Vector2 position)
    {
        GameObject go = new GameObject("Player");
        go.transform.position = position;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        // PlayerController builds its own collider and animated sprite rig.
        go.AddComponent<PlayerController>();

        // Give the camera something to follow.
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = go.transform;
        }
        return go;
    }

    // ----- Glow helper ---------------------------------------------------

    SpriteRenderer MakeGlow(Transform parent, Vector3 localPos, float diameter,
        Color color, int order)
    {
        return MakeGlow(parent, localPos, diameter, color, order, new Vector2(diameter, diameter));
    }

    SpriteRenderer MakeGlow(Transform parent, Vector3 localPos, float diameter,
        Color color, int order, Vector2 worldSize)
    {
        GameObject go = new GameObject("Glow");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = glowSprite != null ? glowSprite : squareSprite;
        sr.color = color;
        sr.sortingOrder = order;

        float unit = (glowSprite != null) ? glowSprite.bounds.size.y : 1f;
        // Convert desired world size into the parent's local scale.
        Vector3 pScale = parent != null ? parent.lossyScale : Vector3.one;
        go.transform.localScale = new Vector3(
            worldSize.x / unit / Mathf.Max(0.0001f, pScale.x),
            worldSize.y / unit / Mathf.Max(0.0001f, pScale.y), 1f);
        return sr;
    }
}

// =====================================================================
//  Small reusable behaviours (attached in code, so no extra .cs/.meta
//  files are needed). Kept here to keep the project's script count low.
// =====================================================================

/// <summary>Rotates its transform at a constant speed.</summary>
public class Spinner : MonoBehaviour
{
    public float degreesPerSecond = 90f;
    void Update() => transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
}

/// <summary>Gently bobs up and down around the start position.</summary>
public class Bobber : MonoBehaviour
{
    public float amplitude = 0.18f;
    public float speed = 2.2f;
    private Vector3 start;
    private float phase;
    void Start() { start = transform.position; phase = Random.value * 6.28f; }
    void Update()
    {
        Vector3 p = transform.position;
        p.y = start.y + Mathf.Sin(Time.time * speed + phase) * amplitude;
        transform.position = p;
    }
}

/// <summary>Pulses a sprite's alpha (and optionally scale) for a living glow.</summary>
public class Pulser : MonoBehaviour
{
    public SpriteRenderer target;   // defaults to a renderer on this object
    public float speed = 3f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;
    public float scalePulse = 0f;   // 0 = no scale pulse
    private Vector3 baseScale;
    private float phase;

    void Start()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (target != null) baseScale = target.transform.localScale;
        phase = Random.value * 6.28f;
    }

    void Update()
    {
        if (target == null) return;
        float t = (Mathf.Sin(Time.time * speed + phase) + 1f) * 0.5f;
        Color c = target.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        target.color = c;
        if (scalePulse > 0f)
            target.transform.localScale = baseScale * (1f + t * scalePulse);
    }
}

/// <summary>Moves relative to the camera to fake depth (a parallax layer).</summary>
public class Parallax : MonoBehaviour
{
    [Range(0f, 1f)] public float factor = 0.4f;
    private Transform cam;
    private Vector3 startPos;
    private Vector3 camStart;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
        startPos = transform.position;
        if (cam != null) camStart = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 delta = cam.position - camStart;
        transform.position = startPos + new Vector3(delta.x * factor, delta.y * factor * 0.5f, 0f);
    }
}

/// <summary>Smooth, damped follow camera with a little look-ahead.</summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.18f;
    public float lookAhead = 1.6f;

    private float fixedY;
    private float fixedZ;
    private Vector3 velocity;
    private float lookX;

    void Start()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Look ahead in the direction the player is moving.
        float desiredLook = 0f;
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.5f)
            desiredLook = Mathf.Sign(rb.linearVelocity.x) * lookAhead;
        lookX = Mathf.Lerp(lookX, desiredLook, Time.deltaTime * 3f);

        Vector3 goal = new Vector3(target.position.x + lookX, fixedY, fixedZ);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }
}

/// <summary>Handles picking up a fragment: score, pop burst, self-destruct.</summary>
public class Collectible : MonoBehaviour
{
    public Color glowColor = Color.yellow;
    private bool taken;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken) return;
        if (other.GetComponent<PlayerController>() == null) return;

        taken = true;
        if (GameManager.Instance != null)
            GameManager.Instance.CollectFragment();

        Effects.SpawnBurst(transform.position, glowColor, 8, 0.9f);
        Destroy(gameObject);
    }
}

/// <summary>A short-lived particle that expands and fades, then removes itself.</summary>
public class Puff : MonoBehaviour
{
    public float life = 0.45f;
    public float growth = 2.2f;
    public Vector3 velocity;
    private float t;
    private SpriteRenderer sr;
    private Vector3 baseScale;
    private float startAlpha;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        if (sr != null) startAlpha = sr.color.a;
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / life);
        transform.position += velocity * Time.deltaTime;
        velocity *= 0.90f;
        transform.localScale = baseScale * (1f + k * growth);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = startAlpha * (1f - k);
            sr.color = c;
        }
        if (t >= life) Destroy(gameObject);
    }
}

/// <summary>
/// Procedurally generated art, built once in code and cached. This keeps the
/// game independent of texture-import settings (Unity kept resetting the shipped
/// glow.png to a non-Sprite "Default" texture, which made every glow render as a
/// hard white square).
/// </summary>
public static class ProcArt
{
    private static Sprite glow;

    /// <summary>A soft radial glow with a fully transparent rim.</summary>
    public static Sprite Glow()
    {
        if (glow != null) return glow;

        const int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;

        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a *= a;                          // soft quadratic falloff to the edge
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        t.Apply();
        glow = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return glow;
    }
}

/// <summary>Spawns quick glow-particle bursts (dust, pickups). Static helper.</summary>
public static class Effects
{
    private static Sprite Glow() => ProcArt.Glow();

    public static void SpawnDust(Vector3 pos, int count, float scale)
    {
        SpawnBurst(pos + Vector3.up * 0.05f, new Color(0.7f, 0.85f, 1f, 0.7f), count, scale, true);
    }

    public static void SpawnBurst(Vector3 pos, Color color, int count, float scale, bool groundHug = false)
    {
        Sprite g = Glow();
        if (g == null) return;
        float unit = g.bounds.size.y;

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Puff");
            go.transform.position = pos;
            float s = scale * Random.Range(0.25f, 0.5f);
            go.transform.localScale = new Vector3(s / unit, s / unit, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = g;
            sr.color = color;
            sr.sortingOrder = 8;

            Puff puff = go.AddComponent<Puff>();
            float ang = groundHug
                ? Random.Range(20f, 160f) * Mathf.Deg2Rad
                : Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = Random.Range(1.5f, 3.5f);
            puff.velocity = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * spd;
            puff.life = Random.Range(0.35f, 0.6f);
        }
    }
}

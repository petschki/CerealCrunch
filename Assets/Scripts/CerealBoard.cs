using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Match-3 board: grid management, swipe input, match detection,
/// cascade resolution, refill, level system and scoring.
public class CerealBoard : MonoBehaviour
{
    [Tooltip("Development helper: resets level progress to level 1 on every start")]
    public bool ResetProgressOnStart = true;

    public int Width = 8;
    public int Height = 8;
    public float SwapDuration = 0.16f;
    public float FallDurationPerCell = 0.09f;
    public float PieceScale = 0.86f;

    static readonly string[] SpriteNames = { "raisin", "hazelnut", "oats", "cranberry", "banana", "blueberry", "cornflake" };
    static readonly string[] CharacterNames = { "Ray Sin", "Hazel Nuts", "Oatis", "Cran Berry", "B-Nana", "Barry Blue", "Corny Flake" };
    const string LevelPrefsKey = "cereal_level";

    // ---------- Level system ----------
    // Balance calibrated via Monte Carlo simulation (sawtooth curve):
    // levels 1-3: 5 types, win rate ~98% → ~68%
    // levels 4-7: 6 types, ~82% → ~42%
    // levels 8+:  7 types, ~76% → declining
    // 4 types are unplayably easy with the square and around-the-corner
    // rules (endless cascades, ~3800 points per move) and are never used.
    struct LevelConfig
    {
        public int TypeCount;
        public int Moves;
        public int TargetScore;
    }

    static LevelConfig GetLevelConfig(int level)
    {
        int typeCount, pointsPerMove;
        if (level <= 3)
        {
            typeCount = 5;
            pointsPerMove = 140 + 60 * (level - 1);
        }
        else if (level <= 7)
        {
            typeCount = 6;
            pointsPerMove = 85 + 20 * (level - 4);
        }
        else
        {
            typeCount = 7;
            pointsPerMove = Mathf.Min(60 + 12 * (level - 8), 120);
        }
        int moves = Mathf.Min(11 + level, 22);
        return new LevelConfig
        {
            TypeCount = typeCount,
            Moves = moves,
            TargetScore = Mathf.RoundToInt(moves * pointsPerMove / 10f) * 10
        };
    }

    /// Board shape mask per level — rotates through six layouts.
    /// Inactive cells hold no pieces; falling pieces pass through gaps.
    static bool[,] BuildShape(int level, int w, int h)
    {
        int kind = (level - 1) % 6;
        var shape = new bool[w, h];
        float cx = (w - 1) / 2f, cy = (h - 1) / 2f;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int left = x, right = w - 1 - x, bottom = y, top = h - 1 - y;
                shape[x, y] = kind switch
                {
                    1 => left + bottom >= 2 && right + bottom >= 2 &&
                         left + top >= 2 && right + top >= 2,                 // rounded corners
                    2 => Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= 4.5f,       // diamond
                    3 => (x >= 2 && x <= w - 3) || (y >= 2 && y <= h - 3),    // plus
                    4 => !(x >= 3 && x <= w - 4 && y >= 3 && y <= h - 4),     // donut
                    5 => !(x >= 2 && x <= w - 3 && y >= h - 3),               // U shape
                    _ => true                                                 // full square
                };
            }
        }
        return shape;
    }

    enum GameState { Playing, Won, Lost }

    Sprite[] cerealSprites;
    Sprite cellSprite;
    CerealPiece[,] grid;
    bool[,] activeCells;
    LevelConfig cfg;
    GameState state;
    int level;
    int movesLeft;
    int score;
    int lastCascade;
    bool busy;
    bool rescueUsed; // the rewarded-ad rescue is available once per level attempt

    CerealPiece dragPiece;
    Vector2 dragStartWorld;
    GameUI ui;
    LevelPathScreen pathScreen;

    // Hint: after some idle time, pulse the pieces of one possible move
    const float HintDelay = 6f;
    float idleTime;
    CerealPiece hintA, hintB;
    Coroutine hintRoutine;

    void Start()
    {
        if (ResetProgressOnStart)
            PlayerPrefs.DeleteKey(LevelPrefsKey);

        _ = AdsManager.Instance; // initialize early so ads are preloaded
        LoadSprites();
        SetupCamera();
        BuildTableBackground();
        ui = GameUI.Create();
        pathScreen = LevelPathScreen.Create();
        LoadLevel(PlayerPrefs.GetInt(LevelPrefsKey, 1));
    }

    void LoadSprites()
    {
        cerealSprites = new Sprite[SpriteNames.Length];
        for (int i = 0; i < SpriteNames.Length; i++)
            cerealSprites[i] = Resources.Load<Sprite>("Cereals/" + SpriteNames[i]);
        cellSprite = Resources.Load<Sprite>("Cereals/cell");
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.985f, 0.945f, 0.87f);

        // The board plus margin must fit in both directions
        float halfHeight = Height / 2f + 1.6f;
        float halfWidth = (Width / 2f + 0.7f) / cam.aspect;
        float size = Mathf.Max(halfHeight, halfWidth);
        cam.orthographicSize = size;

        // Portrait: place the board at the bottom (full width), leaving room
        // for HUD and background at the top. Landscape: keep the board centered.
        float camY;
        if (cam.aspect < 1f)
        {
            const float bottomMargin = 1.3f;
            camY = -0.5f + size - bottomMargin;
        }
        else
        {
            camY = (Height - 1) / 2f + 0.3f;
        }
        cam.transform.position = new Vector3((Width - 1) / 2f, camY, -10f);
    }

    /// Cartoon breakfast scene as background, scaled to cover the camera view.
    void BuildTableBackground()
    {
        // In portrait, use the purpose-composed portrait variant
        var sprite = Camera.main.aspect < 1f
            ? Resources.Load<Sprite>("Cereals/bowl_table_portrait")
            : Resources.Load<Sprite>("Cereals/bowl_table");
        if (sprite == null) sprite = Resources.Load<Sprite>("Cereals/bowl_table");
        if (sprite == null) return;

        Camera cam = Camera.main;
        var go = new GameObject("TableBackground");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -20;

        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;
        Vector2 spriteSize = sprite.bounds.size;
        float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * 1.02f;
        go.transform.localScale = new Vector3(scale, scale, 1f);
    }

    void BuildCellBackground()
    {
        // Rebuilt per level because the shape changes
        var old = transform.Find("CellBackground");
        if (old != null) Destroy(old.gameObject);

        var parent = new GameObject("CellBackground").transform;
        parent.SetParent(transform, false);

        var panelSprite = Resources.Load<Sprite>("Cereals/ui_panel");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (!activeCells[x, y]) continue;

                // Dark backdrop tile per cell; overlapping tiles form the
                // shape's silhouette (opaque so overlaps stay uniform)
                if (panelSprite != null)
                {
                    var panelGo = new GameObject($"Panel {x},{y}");
                    panelGo.transform.SetParent(parent, false);
                    panelGo.transform.position = new Vector3(x, y, 0f);
                    var psr = panelGo.AddComponent<SpriteRenderer>();
                    psr.sprite = panelSprite;
                    psr.drawMode = SpriteDrawMode.Sliced;
                    psr.size = new Vector2(1.26f, 1.26f);
                    psr.color = new Color(0.33f, 0.2f, 0.1f);
                    psr.sortingOrder = -12;
                }

                var go = new GameObject($"Cell {x},{y}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(x, y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.sortingOrder = -10;
                sr.color = (x + y) % 2 == 0
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(1f, 0.96f, 0.86f, 0.95f);
            }
        }
    }

    // ---------- Level loading ----------

    void LoadLevel(int newLevel)
    {
        level = newLevel;
        cfg = GetLevelConfig(level);
        activeCells = BuildShape(level, Width, Height);

        // Scale the target with the playable area: shaped boards have fewer
        // cells than the full 8x8 the balance was calibrated on.
        int activeCount = 0;
        foreach (bool cell in activeCells)
            if (cell) activeCount++;
        cfg.TargetScore = Mathf.Max(100,
            Mathf.RoundToInt(cfg.TargetScore * activeCount / 64f / 10f) * 10);

        movesLeft = cfg.Moves;
        score = 0;
        state = GameState.Playing;
        dragPiece = null;
        rescueUsed = false;
        ResetHint();

        BuildCellBackground();
        ClearAllPieces();
        FillInitialBoard();

        if (!HasPossibleMove())
            ReshuffleTypesOnly();

        ui.HideOverlays();
        ui.SetHud(level, score, cfg.TargetScore, movesLeft);
    }

    void ClearAllPieces()
    {
        if (grid == null) return;
        foreach (var piece in grid)
            if (piece != null)
                Destroy(piece.gameObject);
    }

    void FillInitialBoard()
    {
        grid = new CerealPiece[Width, Height];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (!activeCells[x, y]) continue;
                int type = RandomTypeWithoutMatch(x, y);
                grid[x, y] = SpawnPiece(x, y, type, new Vector3(x, y, 0f));
            }
        }
    }

    /// Random type (from the types allowed in this level) that creates neither
    /// a finished 3-in-a-row nor a 2x2 square. Called while filling from bottom-left
    /// to top-right, so cells to the left and below already have their types set.
    int RandomTypeWithoutMatch(int x, int y)
    {
        var banned = new HashSet<int>();
        if (x >= 2 && grid[x - 1, y] != null && grid[x - 2, y] != null &&
            grid[x - 1, y].Type == grid[x - 2, y].Type)
            banned.Add(grid[x - 1, y].Type);
        if (y >= 2 && grid[x, y - 1] != null && grid[x, y - 2] != null &&
            grid[x, y - 1].Type == grid[x, y - 2].Type)
            banned.Add(grid[x, y - 1].Type);
        if (x >= 1 && y >= 1 &&
            grid[x - 1, y] != null && grid[x - 1, y - 1] != null && grid[x, y - 1] != null &&
            grid[x - 1, y].Type == grid[x - 1, y - 1].Type &&
            grid[x - 1, y].Type == grid[x, y - 1].Type)
            banned.Add(grid[x - 1, y].Type);
        int type;
        do { type = Random.Range(0, cfg.TypeCount); } while (banned.Contains(type));
        return type;
    }

    CerealPiece SpawnPiece(int x, int y, int type, Vector3 worldPos)
    {
        var go = new GameObject($"Piece {SpriteNames[type]}");
        go.transform.SetParent(transform, false);
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * PieceScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cerealSprites[type];
        sr.sortingOrder = 0;
        var piece = go.AddComponent<CerealPiece>();
        piece.SetGridPosition(x, y);
        piece.Type = type;
        return piece;
    }

    // ---------- Input ----------

    void Update()
    {
        if (busy || state != GameState.Playing || AdsManager.IsShowingAd) return;

        idleTime += Time.deltaTime;
        if (idleTime >= HintDelay && hintRoutine == null)
            ShowHint();

        if (Input.GetMouseButtonDown(0))
        {
            ResetHint();
            Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.RoundToInt(world.x);
            int y = Mathf.RoundToInt(world.y);
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                dragPiece = grid[x, y];
                dragStartWorld = world;
            }
        }
        else if (Input.GetMouseButton(0) && dragPiece != null)
        {
            Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 delta = world - dragStartWorld;
            if (delta.magnitude > 0.35f)
            {
                int dx = 0, dy = 0;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) dx = delta.x > 0 ? 1 : -1;
                else dy = delta.y > 0 ? 1 : -1;

                int tx = dragPiece.X + dx;
                int ty = dragPiece.Y + dy;
                if (tx >= 0 && tx < Width && ty >= 0 && ty < Height && grid[tx, ty] != null)
                    StartCoroutine(SwapRoutine(dragPiece, grid[tx, ty]));
                dragPiece = null;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            dragPiece = null;
        }
    }

    // ---------- Game flow ----------

    IEnumerator SwapRoutine(CerealPiece a, CerealPiece b)
    {
        busy = true;
        lastCascade = 0;

        SwapInGrid(a, b);
        a.MoveTo(new Vector3(a.X, a.Y, 0f), SwapDuration);
        b.MoveTo(new Vector3(b.X, b.Y, 0f), SwapDuration);
        yield return WaitForPieces();

        if (FindMatches().Count > 0)
        {
            movesLeft--;
            ui.SetHud(level, score, cfg.TargetScore, movesLeft);
            yield return ResolveBoard();
            EvaluateLevelEnd();
        }
        else
        {
            // Invalid move: swap back, does not cost a move
            SwapInGrid(a, b);
            a.MoveTo(new Vector3(a.X, a.Y, 0f), SwapDuration);
            b.MoveTo(new Vector3(b.X, b.Y, 0f), SwapDuration);
            yield return WaitForPieces();
            yield return StartCoroutine(a.ShakeRoutine());
        }

        if (state == GameState.Playing && !HasPossibleMove())
            yield return ReshuffleRoutine();

        busy = false;
    }

    void EvaluateLevelEnd()
    {
        if (score >= cfg.TargetScore)
        {
            state = GameState.Won;
            PlayerPrefs.SetInt(LevelPrefsKey, level + 1);
            PlayerPrefs.Save();
            ui.ShowWin(level + 1, () =>
                AdsManager.Instance.MaybeShowInterstitial(level, () =>
                {
                    // Path screen with Cerealia's hop, then the next level
                    ui.HideOverlays();
                    pathScreen.Show(level, level + 1, () => LoadLevel(level + 1));
                }));
        }
        else if (movesLeft <= 0)
        {
            state = GameState.Lost;
            ui.ShowLose(
                !rescueUsed && AdsManager.Instance.RewardedAvailable,
                () => AdsManager.Instance.ShowRewarded(earned =>
                {
                    if (!earned) return;
                    rescueUsed = true;
                    movesLeft += 5;
                    state = GameState.Playing;
                    ui.HideOverlays();
                    ui.SetHud(level, score, cfg.TargetScore, movesLeft);
                }),
                () => LoadLevel(level));
        }
    }

    void SwapInGrid(CerealPiece a, CerealPiece b)
    {
        grid[a.X, a.Y] = b;
        grid[b.X, b.Y] = a;
        int ax = a.X, ay = a.Y;
        a.SetGridPosition(b.X, b.Y);
        b.SetGridPosition(ax, ay);
    }

    IEnumerator ResolveBoard()
    {
        while (true)
        {
            var matches = FindMatches();
            if (matches.Count == 0) break;

            lastCascade++;
            score += matches.Count * 10 * lastCascade;
            ui.SetHud(level, score, cfg.TargetScore, movesLeft);
            ShowCharacterCallout(matches);

            foreach (var piece in matches)
            {
                grid[piece.X, piece.Y] = null;
                StartCoroutine(piece.ClearRoutine());
            }
            yield return new WaitForSeconds(0.25f);
            foreach (var piece in matches)
                Destroy(piece.gameObject);

            yield return CollapseAndRefill();
        }
    }

    /// On a big match (5+ pieces of one type) the character calls out
    /// its name — popup at the group's centroid.
    void ShowCharacterCallout(List<CerealPiece> matches)
    {
        var byType = new Dictionary<int, List<CerealPiece>>();
        foreach (var piece in matches)
        {
            if (!byType.TryGetValue(piece.Type, out var list))
                byType[piece.Type] = list = new List<CerealPiece>();
            list.Add(piece);
        }

        List<CerealPiece> best = null;
        foreach (var group in byType.Values)
            if (best == null || group.Count > best.Count)
                best = group;

        if (best == null || best.Count < 5) return;

        Vector3 center = Vector3.zero;
        foreach (var piece in best)
            center += piece.transform.position;
        center /= best.Count;

        FloatingText.Spawn(center, CharacterNames[best[0].Type] + "!");
    }

    List<CerealPiece> FindMatches()
    {
        bool[,] matched = new bool[Width, Height];

        // 1) Straight runs of 3+ (horizontal and vertical)
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width - 2; x++)
            {
                var a = grid[x, y];
                if (a == null) continue;
                int run = 1;
                while (x + run < Width && grid[x + run, y] != null && grid[x + run, y].Type == a.Type) run++;
                if (run >= 3)
                    for (int i = 0; i < run; i++) matched[x + i, y] = true;
                x += run - 1;
            }
        }
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height - 2; y++)
            {
                var a = grid[x, y];
                if (a == null) continue;
                int run = 1;
                while (y + run < Height && grid[x, y + run] != null && grid[x, y + run].Type == a.Type) run++;
                if (run >= 3)
                    for (int i = 0; i < run; i++) matched[x, y + i] = true;
                y += run - 1;
            }
        }

        // 2) Squares/rectangles: every 2x2 block of the same type.
        //    Larger rectangles consist of overlapping 2x2 blocks.
        for (int x = 0; x < Width - 1; x++)
        {
            for (int y = 0; y < Height - 1; y++)
            {
                var a = grid[x, y];
                if (a == null) continue;
                if (grid[x + 1, y] != null && grid[x + 1, y].Type == a.Type &&
                    grid[x, y + 1] != null && grid[x, y + 1].Type == a.Type &&
                    grid[x + 1, y + 1] != null && grid[x + 1, y + 1].Type == a.Type)
                {
                    matched[x, y] = matched[x + 1, y] = matched[x, y + 1] = matched[x + 1, y + 1] = true;
                }
            }
        }

        // 3) Around-the-corner extension: a run of 2+ of the same type joins
        //    the match as soon as one of its pieces is already matched
        //    (fixpoint loop so chains across multiple corners are included).
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width - 1; x++)
                {
                    var a = grid[x, y];
                    if (a == null) continue;
                    int run = 1;
                    while (x + run < Width && grid[x + run, y] != null && grid[x + run, y].Type == a.Type) run++;
                    if (run >= 2 && ExtendRun(matched, x, y, run, true)) changed = true;
                    x += run - 1;
                }
            }
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height - 1; y++)
                {
                    var a = grid[x, y];
                    if (a == null) continue;
                    int run = 1;
                    while (y + run < Height && grid[x, y + run] != null && grid[x, y + run].Type == a.Type) run++;
                    if (run >= 2 && ExtendRun(matched, x, y, run, false)) changed = true;
                    y += run - 1;
                }
            }
        }

        var result = new List<CerealPiece>();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (matched[x, y] && grid[x, y] != null)
                    result.Add(grid[x, y]);
        return result;
    }

    /// Marks a complete run if it is partially (but not fully) matched.
    /// Returns true if new pieces were added.
    bool ExtendRun(bool[,] matched, int x, int y, int run, bool horizontal)
    {
        bool any = false, all = true;
        for (int i = 0; i < run; i++)
        {
            bool m = horizontal ? matched[x + i, y] : matched[x, y + i];
            any |= m;
            all &= m;
        }
        if (!any || all) return false;

        for (int i = 0; i < run; i++)
        {
            if (horizontal) matched[x + i, y] = true;
            else matched[x, y + i] = true;
        }
        return true;
    }

    IEnumerator CollapseAndRefill()
    {
        for (int x = 0; x < Width; x++)
        {
            // Active slots of this column, bottom to top. Pieces fall straight
            // down and pass through inactive gaps in the shape.
            var slots = new List<int>();
            for (int y = 0; y < Height; y++)
                if (activeCells[x, y]) slots.Add(y);

            var falling = new List<CerealPiece>();
            foreach (int y in slots)
            {
                if (grid[x, y] != null) falling.Add(grid[x, y]);
                grid[x, y] = null;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                int targetY = slots[i];
                if (i < falling.Count)
                {
                    var piece = falling[i];
                    grid[x, targetY] = piece;
                    if (piece.Y != targetY)
                    {
                        float distance = piece.Y - targetY;
                        piece.SetGridPosition(x, targetY);
                        piece.MoveTo(new Vector3(x, targetY, 0f), FallDurationPerCell * distance + 0.05f);
                    }
                }
                else
                {
                    // Spawn new pieces stacked above the board
                    int spawnRow = Height + (i - falling.Count);
                    int type = Random.Range(0, cfg.TypeCount);
                    var piece = SpawnPiece(x, targetY, type, new Vector3(x, spawnRow, 0f));
                    grid[x, targetY] = piece;
                    piece.MoveTo(new Vector3(x, targetY, 0f), FallDurationPerCell * (spawnRow - targetY) + 0.05f);
                }
            }
        }
        yield return WaitForPieces();
    }

    IEnumerator WaitForPieces()
    {
        bool moving = true;
        while (moving)
        {
            yield return null;
            moving = false;
            foreach (var piece in grid)
                if (piece != null && piece.Moving) { moving = true; break; }
        }
    }

    // ---------- Hint ----------

    /// Picks a random valid move and pulses both pieces until the player acts.
    void ShowHint()
    {
        var candidates = new List<(CerealPiece a, CerealPiece b)>();
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (grid[x, y] == null) continue;
                if (x < Width - 1 && grid[x + 1, y] != null && SwapCreatesMatch(x, y, x + 1, y))
                    candidates.Add((grid[x, y], grid[x + 1, y]));
                if (y < Height - 1 && grid[x, y + 1] != null && SwapCreatesMatch(x, y, x, y + 1))
                    candidates.Add((grid[x, y], grid[x, y + 1]));
            }
        }
        if (candidates.Count == 0) return;

        var pick = candidates[Random.Range(0, candidates.Count)];
        hintA = pick.a;
        hintB = pick.b;
        hintRoutine = StartCoroutine(HintPulseRoutine());
    }

    IEnumerator HintPulseRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float pulse = PieceScale * (1f + 0.12f * Mathf.Abs(Mathf.Sin(t * 4f)));
            if (hintA != null && !hintA.Moving) hintA.transform.localScale = Vector3.one * pulse;
            if (hintB != null && !hintB.Moving) hintB.transform.localScale = Vector3.one * pulse;
            yield return null;
        }
    }

    void ResetHint()
    {
        idleTime = 0f;
        if (hintRoutine != null)
        {
            StopCoroutine(hintRoutine);
            hintRoutine = null;
        }
        if (hintA != null) hintA.transform.localScale = Vector3.one * PieceScale;
        if (hintB != null) hintB.transform.localScale = Vector3.one * PieceScale;
        hintA = null;
        hintB = null;
    }

    // ---------- Deadlock handling ----------

    bool HasPossibleMove()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (grid[x, y] == null) continue;
                if (x < Width - 1 && grid[x + 1, y] != null && SwapCreatesMatch(x, y, x + 1, y)) return true;
                if (y < Height - 1 && grid[x, y + 1] != null && SwapCreatesMatch(x, y, x, y + 1)) return true;
            }
        }
        return false;
    }

    bool SwapCreatesMatch(int x1, int y1, int x2, int y2)
    {
        int t1 = grid[x1, y1].Type;
        int t2 = grid[x2, y2].Type;
        grid[x1, y1].Type = t2;
        grid[x2, y2].Type = t1;
        bool match = HasMatchAt(x1, y1) || HasMatchAt(x2, y2);
        grid[x1, y1].Type = t1;
        grid[x2, y2].Type = t2;
        return match;
    }

    bool HasMatchAt(int x, int y)
    {
        int type = grid[x, y].Type;

        int run = 1;
        for (int i = x - 1; i >= 0 && grid[i, y] != null && grid[i, y].Type == type; i--) run++;
        for (int i = x + 1; i < Width && grid[i, y] != null && grid[i, y].Type == type; i++) run++;
        if (run >= 3) return true;

        run = 1;
        for (int i = y - 1; i >= 0 && grid[x, i] != null && grid[x, i].Type == type; i--) run++;
        for (int i = y + 1; i < Height && grid[x, i] != null && grid[x, i].Type == type; i++) run++;
        if (run >= 3) return true;

        // 2x2 squares: check all four blocks that could contain (x,y)
        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dy = -1; dy <= 0; dy++)
            {
                int x0 = x + dx, y0 = y + dy;
                if (x0 < 0 || y0 < 0 || x0 + 1 >= Width || y0 + 1 >= Height) continue;
                if (grid[x0, y0] != null && grid[x0, y0].Type == type &&
                    grid[x0 + 1, y0] != null && grid[x0 + 1, y0].Type == type &&
                    grid[x0, y0 + 1] != null && grid[x0, y0 + 1].Type == type &&
                    grid[x0 + 1, y0 + 1] != null && grid[x0 + 1, y0 + 1].Type == type)
                    return true;
            }
        }
        return false;
    }

    void ReshuffleTypesOnly()
    {
        do
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (grid[x, y] != null)
                        grid[x, y].Type = RandomTypeWithoutMatch(x, y);
        }
        while (!HasPossibleMove());

        foreach (var piece in grid)
            if (piece != null)
                piece.Renderer.sprite = cerealSprites[piece.Type];
    }

    IEnumerator ReshuffleRoutine()
    {
        ReshuffleTypesOnly();
        foreach (var piece in grid)
            if (piece != null)
                StartCoroutine(piece.ShakeRoutine());
        yield return new WaitForSeconds(0.3f);
    }

}

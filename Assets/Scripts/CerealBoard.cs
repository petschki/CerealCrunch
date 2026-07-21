using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Match-3-Board: Grid-Verwaltung, Eingabe (Swipe), Match-Erkennung,
/// Auflösen mit Kaskaden, Nachfüllen, Level-System und Punktestand.
public class CerealBoard : MonoBehaviour
{
    [Tooltip("Für die Entwicklung: setzt den Levelfortschritt bei jedem Start auf Level 1 zurück")]
    public bool ResetProgressOnStart = true;

    public int Width = 8;
    public int Height = 8;
    public float SwapDuration = 0.16f;
    public float FallDurationPerCell = 0.09f;
    public float PieceScale = 0.86f;

    static readonly string[] SpriteNames = { "raisin", "hazelnut", "oats", "cranberry", "banana", "blueberry", "cornflake" };
    static readonly string[] CharacterNames = { "Ray Sin", "Hazel Nuts", "Oatis", "Cran Berry", "B-Nana", "Barry Blue", "Corny Flake" };
    const string LevelPrefsKey = "cereal_level";

    // ---------- Level-System ----------
    // Balance per Monte-Carlo-Simulation kalibriert (Sägezahn-Kurve):
    // Level 1-3: 5 Sorten, Gewinnrate ~98% → ~68%
    // Level 4-7: 6 Sorten, ~82% → ~42%
    // Level 8+:  7 Sorten, ~76% → fallend
    // 4 Sorten sind mit Quadrat- und Über-Eck-Regeln unspielbar leicht
    // (Endlos-Kaskaden, ~3800 Punkte/Zug) und werden nicht verwendet.
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

    enum GameState { Playing, Won, Lost }

    Sprite[] cerealSprites;
    Sprite cellSprite;
    CerealPiece[,] grid;
    LevelConfig cfg;
    GameState state;
    int level;
    int movesLeft;
    int score;
    int lastCascade;
    bool busy;
    bool rescueUsed; // Rewarded-Rettung gibt es nur einmal pro Levelversuch

    CerealPiece dragPiece;
    Vector2 dragStartWorld;

    void Start()
    {
        if (ResetProgressOnStart)
            PlayerPrefs.DeleteKey(LevelPrefsKey);

        _ = AdsManager.Instance; // früh initialisieren, damit Anzeigen vorgeladen sind
        LoadSprites();
        SetupCamera();
        BuildTableBackground();
        BuildCellBackground();
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

        // Board plus Rand muss in beide Richtungen passen
        float halfHeight = Height / 2f + 1.6f;
        float halfWidth = (Width / 2f + 0.7f) / cam.aspect;
        float size = Mathf.Max(halfHeight, halfWidth);
        cam.orthographicSize = size;

        // Hochformat: Board unten platzieren (volle Breite), oben Luft für
        // HUD und Hintergrund. Querformat: Board mittig wie bisher.
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

    /// Müslischüssel-Foto als Hintergrund, skaliert so, dass es den Kamerabereich abdeckt.
    void BuildTableBackground()
    {
        // Im Hochformat die eigens komponierte Portrait-Variante verwenden
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
        var parent = new GameObject("CellBackground").transform;
        parent.SetParent(transform, false);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                var go = new GameObject($"Cell {x},{y}");
                go.transform.SetParent(parent, false);
                go.transform.position = new Vector3(x, y, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = cellSprite;
                sr.sortingOrder = -10;
                sr.color = (x + y) % 2 == 0
                    ? new Color(1f, 1f, 1f, 0.72f)
                    : new Color(1f, 0.95f, 0.82f, 0.72f);
            }
        }
    }

    // ---------- Level laden ----------

    void LoadLevel(int newLevel)
    {
        level = newLevel;
        cfg = GetLevelConfig(level);
        movesLeft = cfg.Moves;
        score = 0;
        state = GameState.Playing;
        dragPiece = null;
        rescueUsed = false;

        ClearAllPieces();
        FillInitialBoard();

        if (!HasPossibleMove())
            ReshuffleTypesOnly();
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
                int type = RandomTypeWithoutMatch(x, y);
                grid[x, y] = SpawnPiece(x, y, type, new Vector3(x, y, 0f));
            }
        }
    }

    /// Zufälliger Typ (aus den im Level erlaubten Sorten), der weder ein fertiges
    /// 3er-Match noch ein 2x2-Quadrat erzeugt. Wird beim Befüllen von links unten
    /// nach rechts oben aufgerufen, daher sind links und unterhalb schon Typen gesetzt.
    int RandomTypeWithoutMatch(int x, int y)
    {
        var banned = new HashSet<int>();
        if (x >= 2 && grid[x - 1, y].Type == grid[x - 2, y].Type) banned.Add(grid[x - 1, y].Type);
        if (y >= 2 && grid[x, y - 1].Type == grid[x, y - 2].Type) banned.Add(grid[x, y - 1].Type);
        if (x >= 1 && y >= 1 &&
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

    // ---------- Eingabe ----------

    void Update()
    {
        if (busy || state != GameState.Playing || AdsManager.IsShowingAd) return;

        if (Input.GetMouseButtonDown(0))
        {
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
                if (tx >= 0 && tx < Width && ty >= 0 && ty < Height)
                    StartCoroutine(SwapRoutine(dragPiece, grid[tx, ty]));
                dragPiece = null;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            dragPiece = null;
        }
    }

    // ---------- Spielablauf ----------

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
            yield return ResolveBoard();
            EvaluateLevelEnd();
        }
        else
        {
            // Ungültiger Zug: zurücktauschen, kostet keinen Zug
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
        }
        else if (movesLeft <= 0)
        {
            state = GameState.Lost;
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

    /// Bei einem großen Match (ab 5 Teilen einer Sorte) ruft der Charakter
    /// seinen Namen — Popup am Schwerpunkt der Gruppe.
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

        // 1) Gerade Reihen ab 3 (horizontal und vertikal)
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

        // 2) Quadrate/Rechtecke: jeder 2x2-Block gleicher Sorte.
        //    Größere Rechtecke bestehen aus überlappenden 2x2-Blöcken.
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

        // 3) Über-Eck-Erweiterung: eine Reihe ab 2 gleicher Sorte kommt dazu,
        //    sobald eines ihrer Teile bereits gematcht ist (Fixpunkt-Schleife,
        //    damit auch Ketten über mehrere Ecken mitgenommen werden).
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

    /// Markiert eine komplette Reihe, wenn sie teilweise (aber nicht ganz) gematcht ist.
    /// Liefert true, wenn dabei neue Teile hinzugekommen sind.
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
            int writeY = 0;
            for (int y = 0; y < Height; y++)
            {
                if (grid[x, y] == null) continue;
                if (y != writeY)
                {
                    var piece = grid[x, y];
                    grid[x, writeY] = piece;
                    grid[x, y] = null;
                    piece.SetGridPosition(x, writeY);
                    piece.MoveTo(new Vector3(x, writeY, 0f), FallDurationPerCell * (y - writeY) + 0.05f);
                }
                writeY++;
            }
            int spawnOffset = 1;
            for (int y = writeY; y < Height; y++)
            {
                int type = Random.Range(0, cfg.TypeCount);
                var piece = SpawnPiece(x, y, type, new Vector3(x, Height + spawnOffset - 1, 0f));
                grid[x, y] = piece;
                piece.MoveTo(new Vector3(x, y, 0f), FallDurationPerCell * (Height + spawnOffset - 1 - y) + 0.05f);
                spawnOffset++;
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

    // ---------- Deadlock-Behandlung ----------

    bool HasPossibleMove()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (x < Width - 1 && SwapCreatesMatch(x, y, x + 1, y)) return true;
                if (y < Height - 1 && SwapCreatesMatch(x, y, x, y + 1)) return true;
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
        for (int i = x - 1; i >= 0 && grid[i, y].Type == type; i--) run++;
        for (int i = x + 1; i < Width && grid[i, y].Type == type; i++) run++;
        if (run >= 3) return true;

        run = 1;
        for (int i = y - 1; i >= 0 && grid[x, i].Type == type; i--) run++;
        for (int i = y + 1; i < Height && grid[x, i].Type == type; i++) run++;
        if (run >= 3) return true;

        // 2x2-Quadrate: alle vier Blöcke prüfen, in denen (x,y) liegen kann
        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dy = -1; dy <= 0; dy++)
            {
                int x0 = x + dx, y0 = y + dy;
                if (x0 < 0 || y0 < 0 || x0 + 1 >= Width || y0 + 1 >= Height) continue;
                if (grid[x0, y0].Type == type &&
                    grid[x0 + 1, y0].Type == type &&
                    grid[x0, y0 + 1].Type == type &&
                    grid[x0 + 1, y0 + 1].Type == type)
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
                    grid[x, y].Type = RandomTypeWithoutMatch(x, y);
        }
        while (!HasPossibleMove());

        foreach (var piece in grid)
            piece.Renderer.sprite = cerealSprites[piece.Type];
    }

    IEnumerator ReshuffleRoutine()
    {
        ReshuffleTypesOnly();
        foreach (var piece in grid)
            StartCoroutine(piece.ShakeRoutine());
        yield return new WaitForSeconds(0.3f);
    }

    // ---------- UI ----------

    void OnGUI()
    {
        if (AdsManager.IsShowingAd) return; // der FakeAdsProvider zeichnet gerade

        var title = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.04f),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        title.normal.textColor = new Color(0.42f, 0.26f, 0.13f);

        var info = new GUIStyle(title)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.028f)
        };
        info.normal.textColor = new Color(0.55f, 0.35f, 0.16f);

        GUI.Label(new Rect(0, Screen.height * 0.01f, Screen.width, Screen.height * 0.05f),
            $"CEREAL CRUNCH — Level {level}", title);
        GUI.Label(new Rect(0, Screen.height * 0.055f, Screen.width, Screen.height * 0.04f),
            $"Score: {score} / {cfg.TargetScore}     Züge: {movesLeft}", info);

        if (state == GameState.Playing) return;

        // Abdunkeln + Overlay bei Sieg/Niederlage
        var oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;

        var big = new GUIStyle(title) { fontSize = Mathf.RoundToInt(Screen.height * 0.06f) };
        big.normal.textColor = Color.white;

        var button = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(Screen.height * 0.032f),
            fontStyle = FontStyle.Bold
        };

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        if (state == GameState.Won)
        {
            GUI.Label(new Rect(0, cy - Screen.height * 0.15f, Screen.width, Screen.height * 0.1f),
                "Level geschafft!", big);
            if (GUI.Button(new Rect(cx - 160, cy, 320, Screen.height * 0.07f), $"Weiter zu Level {level + 1}", button))
                AdsManager.Instance.MaybeShowInterstitial(level, () => LoadLevel(level + 1));
        }
        else
        {
            GUI.Label(new Rect(0, cy - Screen.height * 0.15f, Screen.width, Screen.height * 0.1f),
                "Keine Züge mehr!", big);

            float buttonY = cy;
            if (!rescueUsed && AdsManager.Instance.RewardedAvailable)
            {
                if (GUI.Button(new Rect(cx - 160, buttonY, 320, Screen.height * 0.07f),
                        "+5 Züge — Werbung ansehen", button))
                {
                    AdsManager.Instance.ShowRewarded(earned =>
                    {
                        if (earned)
                        {
                            rescueUsed = true;
                            movesLeft += 5;
                            state = GameState.Playing;
                        }
                    });
                }
                buttonY += Screen.height * 0.09f;
            }
            if (GUI.Button(new Rect(cx - 160, buttonY, 320, Screen.height * 0.07f), "Nochmal versuchen", button))
                LoadLevel(level);
        }
    }
}

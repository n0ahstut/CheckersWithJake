using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    [Header("Sprites - Assign in Inspector")]
    public Sprite squareSprite;
    public Sprite pieceSprite;

    [Header("AI Settings")]
    public int aiSearchDepth = 5;

    Color lightSquare = new Color(0.93f, 0.84f, 0.7f);
    Color darkSquare = new Color(0.55f, 0.27f, 0.07f);
    Color blackPiece = new Color(0.15f, 0.15f, 0.15f);
    Color whitePiece = new Color(0.95f, 0.95f, 0.9f);
    Color selectedColor = new Color(1f, 1f, 0.3f, 0.5f);
    Color moveHintColor = new Color(0.3f, 1f, 0.3f, 0.5f);
    Color kingMarker = new Color(1f, 0.84f, 0f);

    CheckersBoard board;
    CheckersAI ai;
    int aiPlayer;
    int humanPlayer;
    int currentPlayer;
    bool gameOver = false;
    bool boardFlipped = false; // flip board so human pieces are always at the bottom

    int selectedRow = -1;
    int selectedCol = -1;
    List<Move> currentLegalMoves;
    List<Move> selectedPieceMoves;

    GameObject[,] squareObjects;
    GameObject[,] pieceObjects;
    GameObject[,] highlightObjects;
    GameObject[,] kingCrownObjects;

    Text statusText;
    GameObject menuPanel;
    GameObject gamePanel;

    void Start()
    {
        SetupCamera();
        GameObject canvasObj = BuildCanvas();
        BuildMenuUI(canvasObj);
        BuildGameUI(canvasObj);
        ShowMenu();
    }

    void SetupCamera()
    {
        Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10f);
        Camera.main.orthographicSize = 5f;
        Camera.main.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
    }

    GameObject BuildCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        return canvasObj;
    }

    void BuildMenuUI(GameObject canvasObj)
    {
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform mpRect = menuPanel.AddComponent<RectTransform>();
        mpRect.anchorMin = Vector2.zero;
        mpRect.anchorMax = Vector2.one;
        mpRect.offsetMin = Vector2.zero;
        mpRect.offsetMax = Vector2.zero;

        CreateUIText(menuPanel.transform, "Checkers AI", 52, new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.92f));

        CreateUIButton(menuPanel.transform, "Play as White  (AI goes first)", new Vector2(0.2f, 0.44f), new Vector2(0.8f, 0.57f), () =>
        {
            humanPlayer = CheckersBoard.WHITE;
            StartGame();
        });

        CreateUIButton(menuPanel.transform, "Play as Black  (You go first)", new Vector2(0.2f, 0.27f), new Vector2(0.8f, 0.40f), () =>
        {
            humanPlayer = CheckersBoard.BLACK;
            StartGame();
        });
    }

    void BuildGameUI(GameObject canvasObj)
    {
        gamePanel = new GameObject("GamePanel");
        gamePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform gpRect = gamePanel.AddComponent<RectTransform>();
        gpRect.anchorMin = Vector2.zero;
        gpRect.anchorMax = Vector2.one;
        gpRect.offsetMin = Vector2.zero;
        gpRect.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(gamePanel.transform, false);
        statusText = textObj.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 28;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.92f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        CreateUIButton(gamePanel.transform, "Restart", new Vector2(0.08f, 0.44f), new Vector2(0.26f, 0.56f), OnRestart,
            new Color(0.75f, 0.2f, 0.2f));
    }

    void ShowMenu()
    {
        CancelInvoke();
        if (squareObjects != null)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if (squareObjects[r, c] != null) Destroy(squareObjects[r, c]);
                    if (pieceObjects[r, c] != null) Destroy(pieceObjects[r, c]);
                    if (highlightObjects[r, c] != null) Destroy(highlightObjects[r, c]);
                    if (kingCrownObjects[r, c] != null) Destroy(kingCrownObjects[r, c]);
                }
            squareObjects = null;
            pieceObjects = null;
            highlightObjects = null;
            kingCrownObjects = null;
        }
        gameOver = false;
        menuPanel.SetActive(true);
        gamePanel.SetActive(false);
    }

    void StartGame()
    {
        menuPanel.SetActive(false);
        gamePanel.SetActive(true);

        currentPlayer = CheckersBoard.BLACK;
        aiPlayer = (humanPlayer == CheckersBoard.WHITE) ? CheckersBoard.BLACK : CheckersBoard.WHITE;
        boardFlipped = (humanPlayer == CheckersBoard.BLACK);

        board = new CheckersBoard();
        ai = new CheckersAI(aiPlayer, aiSearchDepth);

        gameOver = false;
        selectedRow = -1;
        selectedCol = -1;
        selectedPieceMoves = null;
        CreateBoard();
        RefreshPieces();
        UpdateStatus();

        if (currentPlayer == aiPlayer)
            Invoke("DoAITurn", 0.3f);
    }

    // When the human plays Black the board is flipped so their pieces appear at the bottom.
    float VisualY(int row) => boardFlipped ? row : 7 - row;

    void CreateBoard()
    {
        squareObjects = new GameObject[8, 8];
        pieceObjects = new GameObject[8, 8];
        highlightObjects = new GameObject[8, 8];
        kingCrownObjects = new GameObject[8, 8];

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                GameObject sq = new GameObject("Square_" + r + "_" + c);
                sq.transform.position = new Vector3(c, VisualY(r), 0);
                SpriteRenderer sr = sq.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                sr.color = (r + c) % 2 == 0 ? lightSquare : darkSquare;
                sr.sortingOrder = 0;
                squareObjects[r, c] = sq;

                GameObject hl = new GameObject("Highlight_" + r + "_" + c);
                hl.transform.position = new Vector3(c, VisualY(r), 0);
                SpriteRenderer hlsr = hl.AddComponent<SpriteRenderer>();
                hlsr.sprite = squareSprite;
                hlsr.color = Color.clear;
                hlsr.sortingOrder = 1;
                highlightObjects[r, c] = hl;
            }
        }
    }

    void RefreshPieces()
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (pieceObjects[r, c] != null) Destroy(pieceObjects[r, c]);
                if (kingCrownObjects[r, c] != null) Destroy(kingCrownObjects[r, c]);
                pieceObjects[r, c] = null;
                kingCrownObjects[r, c] = null;
            }
        }

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                int piece = board.grid[r, c];
                if (piece == CheckersBoard.EMPTY) continue;

                GameObject p = new GameObject("Piece_" + r + "_" + c);
                p.transform.position = new Vector3(c, VisualY(r), 0);
                SpriteRenderer psr = p.AddComponent<SpriteRenderer>();
                psr.sprite = pieceSprite;
                psr.sortingOrder = 2;
                p.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

                psr.color = CheckersBoard.IsBlack(piece) ? blackPiece : whitePiece;
                pieceObjects[r, c] = p;

                if (CheckersBoard.IsKing(piece))
                {
                    GameObject crown = new GameObject("Crown_" + r + "_" + c);
                    crown.transform.position = new Vector3(c, VisualY(r), 0);
                    SpriteRenderer csr = crown.AddComponent<SpriteRenderer>();
                    csr.sprite = pieceSprite;
                    csr.sortingOrder = 3;
                    csr.color = kingMarker;
                    crown.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                    kingCrownObjects[r, c] = crown;
                }
            }
        }
    }

    void ClearHighlights()
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                highlightObjects[r, c].GetComponent<SpriteRenderer>().color = Color.clear;
    }

    void Update()
    {
        if (gameOver) return;
        if (squareObjects == null) return;
        if (currentPlayer == aiPlayer) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int c = Mathf.RoundToInt(worldPos.x);
            int r = boardFlipped ? Mathf.RoundToInt(worldPos.y) : 7 - Mathf.RoundToInt(worldPos.y);

            if (r < 0 || r >= 8 || c < 0 || c >= 8) return;

            HandleClick(r, c);
        }
    }

    void HandleClick(int r, int c)
    {
        currentLegalMoves = board.GetLegalMoves(currentPlayer);

        if (currentLegalMoves.Count == 0)
        {
            int opponent = (currentPlayer == CheckersBoard.BLACK)
                ? CheckersBoard.WHITE : CheckersBoard.BLACK;
            EndGame(opponent);
            return;
        }

        if (CheckersBoard.SameTeam(board.grid[r, c], currentPlayer))
        {
            SelectPiece(r, c);
            return;
        }

        if (selectedRow >= 0 && selectedPieceMoves != null)
        {
            foreach (Move m in selectedPieceMoves)
            {
                int[] end = m.steps[m.steps.Count - 1];
                if (end[0] == r && end[1] == c)
                {
                    DoMove(m);
                    return;
                }
            }
        }
    }

    void SelectPiece(int r, int c)
    {
        ClearHighlights();
        selectedRow = r;
        selectedCol = c;

        highlightObjects[r, c].GetComponent<SpriteRenderer>().color = selectedColor;

        selectedPieceMoves = new List<Move>();
        foreach (Move m in currentLegalMoves)
        {
            if (m.steps[0][0] == r && m.steps[0][1] == c)
                selectedPieceMoves.Add(m);
        }

        foreach (Move m in selectedPieceMoves)
        {
            int[] end = m.steps[m.steps.Count - 1];
            highlightObjects[end[0], end[1]].GetComponent<SpriteRenderer>().color = moveHintColor;
        }
    }

    void DoMove(Move move)
    {
        board.ApplyMove(move);
        ClearHighlights();
        selectedRow = -1;
        selectedCol = -1;
        selectedPieceMoves = null;
        RefreshPieces();

        int opponent = (currentPlayer == CheckersBoard.BLACK)
            ? CheckersBoard.WHITE : CheckersBoard.BLACK;

        if (board.HasNoPieces(opponent) || board.HasNoMoves(opponent))
        {
            EndGame(currentPlayer);
            return;
        }

        currentPlayer = opponent;
        UpdateStatus();

        if (currentPlayer == aiPlayer)
            Invoke("DoAITurn", 0.3f);
    }

    void DoAITurn()
    {
        if (gameOver) return;

        Move bestMove = ai.GetBestMove(board);
        int opponent = (aiPlayer == CheckersBoard.BLACK)
            ? CheckersBoard.WHITE : CheckersBoard.BLACK;

        if (bestMove == null)
        {
            EndGame(opponent);
            return;
        }

        board.ApplyMove(bestMove);
        RefreshPieces();

        Debug.Log("AI searched " + ai.nodesSearched + " nodes");

        if (board.HasNoPieces(opponent) || board.HasNoMoves(opponent))
        {
            EndGame(aiPlayer);
            return;
        }

        currentPlayer = opponent;
        UpdateStatus();
    }

    void EndGame(int winner)
    {
        gameOver = true;
        statusText.text = (winner != aiPlayer) ? "You Win!" : "AI Wins!";
        Invoke("ShowMenu", 2.5f);
    }

    void UpdateStatus()
    {
        if (gameOver) return;
        if (currentPlayer == aiPlayer)
        {
            statusText.text = "AI is thinking...";
        }
        else
        {
            string side = (humanPlayer == CheckersBoard.BLACK) ? "Black" : "White";
            statusText.text = "Your Turn (" + side + ")";
        }
    }

    void OnRestart()
    {
        ShowMenu();
    }

    void CreateUIText(Transform parent, string content, int fontSize, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        Text txt = obj.AddComponent<Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void CreateUIButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action,
        Color? bgColor = null)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor ?? new Color(0.3f, 0.3f, 0.3f);
        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
    }
}

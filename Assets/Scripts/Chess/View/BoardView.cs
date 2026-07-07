using UnityEngine;
using Chess.Core;
using Combat.Selection;
using Combat.Integration;
using TMPro;
using Combat.Core;
using Chess.Integration;
using Combat.View;

namespace Chess.View
{
    [System.Serializable]
    public class PieceModelSet
    {
        public GameObject pawn, knight, bishop, rook, queen, king;

        public GameObject Get(PieceType type) => type switch
        {
            PieceType.Pawn => pawn,
            PieceType.Knight => knight,
            PieceType.Bishop => bishop,
            PieceType.Rook => rook,
            PieceType.Queen => queen,
            PieceType.King => king,
            _ => null
        };
    }

    public class BoardView : MonoBehaviour
    {
        public float squareSize = 1f;
        public Material whiteTileMaterial;
        public Material blackTileMaterial;
        public PieceModelSet whiteModels;
        public PieceModelSet blackModels;
        public float pieceScale = 1f;
        public float pieceYOffset = 0f;

        [Space]

        public CombatView combatView;

        [Space]

        public TextMeshProUGUI turnIndicator;
        public TextMeshProUGUI selectedPieceIndicator;

        // Placeholder highlight colors - swap for a real outline/decal system later.
        static readonly Color EligibleHighlight = new Color(0.3f, 0.9f, 0.4f);
        static readonly Color PickedHighlight = new Color(0.95f, 0.85f, 0.2f);
        static readonly Color OriginHighlight = new Color(0.9f, 0.3f, 0.3f);

        public GameState State { get; private set; }

        // Non-null exactly while a capture's team-selection flow is in progress.
        // BoardInputHandler checks this (via State.Status == AwaitingCombat) to
        // know whether clicks should go to selection instead of normal moves.
        public CaptureTeamSelection ActiveSelection { get; private set; }

        GameObject[,] pieceObjects = new GameObject[8, 8];
        GameObject[,] tileObjects = new GameObject[8, 8];
        Color[,] tileBaseColors = new Color[8, 8];

        void Awake()
        {
            State = new GameState();

            State.OnCaptureTriggered += (from, to) =>
            {
                ActiveSelection = new CaptureTeamSelection(State.Board, from, to);
                RefreshSelectionHighlights();
            };
        }

        void Start()
        {
            BuildTiles();
            RedrawPieces();
            UpdateTurnUI();
        }

        public void UpdateTurnUI()
        {
            if (turnIndicator != null)
            {
                string color = State.SideToMove == PieceColor.White ? "<color=white>White</color>" : "<color=black>Black</color>";
                turnIndicator.text = $"{color}'s Turn";
            }
        }

        public void UpdateSelectedUI(Square? selectedSquare)
        {
            if (selectedPieceIndicator == null) return;

            if (selectedSquare == null || !selectedSquare.Value.IsValid)
            {
                selectedPieceIndicator.text = "Selected: None";
                return;
            }

            var piece = State.Board.Get(selectedSquare.Value);
            if (piece != null)
            {
                string colorStr = piece.Color == PieceColor.White ? "White" : "Black";
                selectedPieceIndicator.text = $"Selected: {colorStr} {piece.Type}";
            }
            else
            {
                selectedPieceIndicator.text = "Selected: None";
            }
        }

        public void ResolveActiveSelection()
        {
            if (ActiveSelection == null || !ActiveSelection.IsReady) return;

            
            var attackerProvider = new PlayerDecisionProvider();
            var defenderProvider = new PlayerDecisionProvider();

            var selection = ActiveSelection;
            ActiveSelection = null;
            ClearSelectionHighlights();

            combatView.gameObject.SetActive(true);

            var combat = CaptureCombatResolver.PrepareCombat(State, selection, attackerProvider, defenderProvider, attackerWon =>
            {
                combatView.gameObject.SetActive(false);
                State.ResolveCapture(attackerWon);
                RedrawPieces();
                UpdateTurnUI();
            });

            combatView.Bind(combat, attackerProvider, defenderProvider);
            combat.Begin();
        }

        public void RefreshSelectionHighlights()
        {
            ClearSelectionHighlights();
            if (ActiveSelection == null) return;

            foreach (var sq in ActiveSelection.CurrentEligible)
                SetTileColor(sq, EligibleHighlight);

            foreach (var sq in ActiveSelection.CurrentPicked)
                SetTileColor(sq, PickedHighlight);

            SetTileColor(ActiveSelection.CurrentOrigin, OriginHighlight);
        }

        public void ClearSelectionHighlights()
        {
            for (int f = 0; f < 8; f++)
                for (int r = 0; r < 8; r++)
                    tileObjects[f, r].GetComponent<Renderer>().material.color = tileBaseColors[f, r];
        }

        void SetTileColor(Square sq, Color color)
        {
            tileObjects[sq.File, sq.Rank].GetComponent<Renderer>().material.color = color;
        }

        void BuildTiles()
        {
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    tile.name = $"Tile_{f}_{r}";
                    tile.transform.SetParent(transform);
                    tile.transform.localScale = Vector3.one * (squareSize / 10f);
                    tile.transform.localPosition = SquareToWorld(new Square(f, r));

                    var mat = (f + r) % 2 == 0 ? blackTileMaterial : whiteTileMaterial;
                    var renderer = tile.GetComponent<Renderer>();
                    if (mat != null) renderer.material = mat;

                    tileObjects[f, r] = tile;
                    tileBaseColors[f, r] = renderer.material.color;
                }
            }
        }

        public Vector3 SquareToWorld(Square sq) => new Vector3(sq.File * squareSize, 0f, sq.Rank * squareSize);

        public Square WorldToSquare(Vector3 world)
        {
            int f = Mathf.RoundToInt(world.x / squareSize);
            int r = Mathf.RoundToInt(world.z / squareSize);
            return new Square(f, r);
        }

        public void RedrawPieces()
        {
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    if (pieceObjects[f, r] != null) Destroy(pieceObjects[f, r]);

                    var piece = State.Board.Squares[f, r];
                    if (piece == null) continue;

                    var modelSet = piece.Color == PieceColor.White ? whiteModels : blackModels;
                    var prefab = modelSet?.Get(piece.Type);

                    GameObject obj;
                    if (prefab != null)
                    {
                        obj = Instantiate(prefab, transform);
                        obj.transform.localScale *= pieceScale;
                    }
                    else
                    {
                        var primitive = piece.Type == PieceType.King ? PrimitiveType.Cylinder : PrimitiveType.Capsule;
                        obj = GameObject.CreatePrimitive(primitive);
                        obj.transform.SetParent(transform);
                        obj.transform.localScale = new Vector3(0.6f, piece.Type == PieceType.Pawn ? 0.4f : 0.6f, 0.6f);
                        obj.GetComponent<Renderer>().material.color = piece.Color == PieceColor.White ? Color.white : Color.gray;
                    }

                    obj.name = $"{piece.Color}_{piece.Type}_{f}_{r}";
                    obj.transform.localPosition = SquareToWorld(new Square(f, r)) + Vector3.up * pieceYOffset;
                    obj.AddComponent<PieceView>().Square = new Square(f, r);
                    pieceObjects[f, r] = obj;
                }
            }
        }
    }
}
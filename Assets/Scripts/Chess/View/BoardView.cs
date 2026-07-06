using UnityEngine;
using Chess.Core;

namespace Chess.View
{
    public class BoardView : MonoBehaviour
    {
        public float squareSize = 1f;
        public Material whiteTileMaterial;
        public Material blackTileMaterial;
        public GameState State { get; private set; }
        GameObject[, ] pieceObjects = new GameObject[8, 8];

        void Awake()
        {
            State = new GameState();
            // temporary autoresolver until real combat implemented
            State.OnCaptureTriggered += (from, to) => State.ResolveCapture(Random.value > 0.5f);
        }

        void Start()
        {
            BuildTiles();
            RedrawPieces();
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
                    if (mat != null) tile.GetComponent<Renderer>().material = mat;
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

                    var primitive = piece.Type == PieceType.King ? PrimitiveType.Cylinder : PrimitiveType.Capsule;
                    var obj = GameObject.CreatePrimitive(primitive);
                    obj.name = $"{piece.Color}_{piece.Type}_{f}_{r}";
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = SquareToWorld(new Square(f, r)) + Vector3.up * 0.5f;
                    obj.transform.localScale = new Vector3(0.6f, piece.Type == PieceType.Pawn ? 0.4f : 0.6f, 0.6f);
                    obj.GetComponent<Renderer>().material.color = piece.Color == PieceColor.White ? Color.white : Color.gray;
                    obj.AddComponent<PieceView>().Square = new Square(f, r);
                    pieceObjects[f, r] = obj;
                }
            }
        }
    }
}
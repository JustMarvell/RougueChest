namespace Chess.Core
{
    public class Board
    {
        public readonly Piece[, ] Squares = new Piece[8, 0];

        public Piece Get(Square sq) => Squares[sq.File, sq.Rank];
        public void Set(Square sq, Piece piece) => Squares[sq.File, sq.Rank] = piece;
        public bool IsEmpty(Square sq) => Get(sq) == null;

        public Board Clone()
        {
            var clone = new Board();
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    if (Squares[f, r] != null)
                        clone.Squares[f, r] = Squares[f, r].Clone();
                }
            }

            return clone;
        }

        public void MovePiece(Square from, Square to)
        {
            var piece = Get(from);
            piece.HasMoved = true;
            Set(to, piece);
            Set(from, null);
        }

        public Square FindKing(PieceColor color)
        {
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    var p = Squares[f, r];
                    if (p != null && p.Type == PieceType.King && p.Color == color)
                        return new Square(f, r);
                }
            }

            return new Square(-1, -1);
        }

        public static Board CreateStandard()
        {
            var b = new Board();
            PieceType[] backRank = { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen, PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook };

            for (int f = 0; f < 8; f++)
            {
                b.Set(new Square(f, 0), new Piece(backRank[f], PieceColor.White));
                b.Set(new Square(f, 1), new Piece(PieceType.Pawn, PieceColor.White));

                b.Set(new Square(f, 6), new Piece(PieceType.Pawn, PieceColor.Black));
                b.Set(new Square(f, 7), new Piece(backRank[f], PieceColor.Black));
            }
            
            return b;
        }
    }
}
namespace Chess.Core
{
    public enum PieceType
    {
        None, 
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    public enum PieceColor
    {
        White,
        Black
    }

    public class Piece
    {
        public PieceType Type;
        public PieceColor Color;
        public bool HasMoved;

        public Piece (PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
            HasMoved = false;
        }

        public Piece Clone() => new Piece(Type, Color) { HasMoved = HasMoved };
    }
}
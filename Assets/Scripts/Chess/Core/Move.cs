namespace Chess.Core
{
    public struct Move
    {
        public Square From;
        public Square To;
        public bool IsCapture;

        public Move(Square from, Square to, bool isCapture)
        {
            From = from;
            To = to;
            IsCapture = isCapture;
        }
    }
}
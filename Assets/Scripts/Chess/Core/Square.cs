namespace Chess.Core
{
    public struct Square
    {
        public int File;
        public int Rank;

        public Square(int file, int rank) { File = file; Rank = rank; }
        public bool IsValid => File >= 0 && File <= 7 && Rank >= 0 && Rank <= 7;

        public override bool Equals(object obj) => obj is Square s && s.File == File && s.Rank == Rank;
        public override int GetHashCode() => File * 8 + Rank;
        public static bool operator ==(Square a, Square b) => a.Equals(b);
        public static bool operator != (Square a, Square b) => !a.Equals(b);
        public override string ToString() => $"{(char)('a' + File)}{Rank + 1}";
    }
}
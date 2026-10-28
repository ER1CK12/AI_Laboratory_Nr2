namespace TicTacToe5
{
    public static class Player
    {
        public const int None  = 0;
        public const int Human = 1;
        public const int AI    = 2;

        public static int Opponent(int p) => 3 - p;
    }
}

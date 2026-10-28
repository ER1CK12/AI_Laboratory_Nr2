namespace TicTacToe5
{
    public sealed class SearchResult
    {
        public int BestMove = -1;
        public float BestScore;
        public int DepthReached;
        public long Nodes;
        public double Milliseconds;
        public bool SolvedExactly;
        public float[] RootScores;
        public bool[] RootIsUpperBound;
    }
}

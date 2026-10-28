using System;

namespace TicTacToe5
{
    public sealed class Board
    {
        public readonly int Size;
        public readonly int WinLength;
        public readonly int CellCount;
        public readonly int[] Cells;
        public readonly int[][] Windows;
        public readonly int[][] CellWindows;
        public readonly float[] LineWeight;
        readonly int[] counts;

        public int EmptyCount { get; private set; }

        public Board(int size, int winLength)
        {
            if (winLength > size) throw new ArgumentException("winLength cannot exceed size");
            Size = size;
            WinLength = winLength;
            CellCount = size * size;
            Cells = new int[CellCount];
            EmptyCount = CellCount;
            int[,] dirs = { { 0, 1 }, { 1, 0 }, { 1, 1 }, { 1, -1 } };
            var windows = new System.Collections.Generic.List<int[]>();
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            for (int d = 0; d < 4; d++)
            {
                int dr = dirs[d, 0], dc = dirs[d, 1];
                int er = r + dr * (winLength - 1), ec = c + dc * (winLength - 1);
                if (er < 0 || er >= size || ec < 0 || ec >= size) continue;
                var w = new int[winLength];
                for (int k = 0; k < winLength; k++) w[k] = (r + dr * k) * size + (c + dc * k);
                windows.Add(w);
            }
            Windows = windows.ToArray();

            var perCell = new System.Collections.Generic.List<int>[CellCount];
            for (int i = 0; i < CellCount; i++) perCell[i] = new System.Collections.Generic.List<int>();
            for (int w = 0; w < Windows.Length; w++)
                foreach (int cell in Windows[w]) perCell[cell].Add(w);
            CellWindows = new int[CellCount][];
            for (int i = 0; i < CellCount; i++) CellWindows[i] = perCell[i].ToArray();

            counts = new int[Windows.Length * 3];
            LineWeight = new float[winLength + 1];
            for (int n = 1; n <= winLength; n++) LineWeight[n] = (float)Math.Pow(8, n - 1);
        }

        Board(Board o)
        {
            Size = o.Size; WinLength = o.WinLength; CellCount = o.CellCount;
            Windows = o.Windows; CellWindows = o.CellWindows; LineWeight = o.LineWeight;
            Cells = (int[])o.Cells.Clone();
            counts = (int[])o.counts.Clone();
            EmptyCount = o.EmptyCount;
        }

        public Board Clone() => new Board(this);

        public int Count(int window, int player) => counts[window * 3 + player];

        public bool Place(int cell, int player)
        {
            Cells[cell] = player;
            EmptyCount--;
            bool win = false;
            foreach (int w in CellWindows[cell])
                if (++counts[w * 3 + player] == WinLength) win = true;
            return win;
        }

        public void Undo(int cell)
        {
            int player = Cells[cell];
            foreach (int w in CellWindows[cell]) counts[w * 3 + player]--;
            Cells[cell] = Player.None;
            EmptyCount++;
        }

        public int FindWinner(out int[] line)
        {
            for (int w = 0; w < Windows.Length; w++)
            {
                if (counts[w * 3 + Player.Human] == WinLength) { line = Windows[w]; return Player.Human; }
                if (counts[w * 3 + Player.AI] == WinLength)    { line = Windows[w]; return Player.AI; }
            }
            line = null;
            return Player.None;
        }

        public const float MaxHeuristic = 5f;

        public float Evaluate(int me)
        {
            int opp = 3 - me;
            float raw = 0f;
            for (int w = 0; w < Windows.Length; w++)
            {
                int a = counts[w * 3 + me], b = counts[w * 3 + opp];
                if (b == 0) raw += LineWeight[a];
                else if (a == 0) raw -= LineWeight[b];
            }
            return MaxHeuristic * raw / (Math.Abs(raw) + 200f);
        }

        public string CellName(int cell) => $"{(char)('A' + cell % Size)}{cell / Size + 1}";
    }
}

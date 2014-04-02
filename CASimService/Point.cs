using System.Collections.Generic;

namespace CASimService {
    /// <summary>
    /// Structure representing a cell's coordinates on a grid
    /// </summary>
    public struct CPoint {
        public int X, Y;
        public CPoint(int x, int y) { X = x; Y = y; }
    }


    /// <summary>
    /// Structure representing a cell to be updated, including its
    /// coordinates and new state.
    /// </summary>
    public struct Cell {
        public int X, Y;
        public uint state;

        public Cell(CPoint cell, uint s) { X = cell.X; Y = cell.Y; state = s; }
        public Cell(int x, int y, uint s) { X = x; Y = y; state = s; }
    }


    /// <summary>
    /// Point comparer for use in the SortedSet containing the set of
    /// points to be checked in the next iteration
    /// </summary>
    public class PointComparer : IComparer<CPoint> {
        public int Compare(CPoint a, CPoint b) {
            if (a.Y == b.Y && a.X == b.X) return 0;
            else if (a.Y > b.Y || (a.Y == b.Y && a.X > b.X)) return 1;
            else return -1;
        }
    }
}

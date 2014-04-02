using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace CASimClient {
    /// <summary>
    /// Structure representing a cell's coordinates on a grid
    /// </summary>
    public struct Point {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
    }


    /// <summary>
    /// Structure representing a cell to be updated, including its
    /// coordinates and new state.
    /// </summary>
    public struct Cell {
        public int X, Y;
        public uint state;

        public Cell(Point cell, uint s) { X = cell.X; Y = cell.Y; state = s; }
        public Cell(int x, int y, uint s) { X = x; Y = y; state = s; }
    }


    /// <summary>
    /// Point comparer for use in the SortedSet containing the set of
    /// points to be checked in the next iteration
    /// </summary>
    public class PointComparer : IComparer<Point> {
        public int Compare(Point a, Point b) {
            if (a.Y == b.Y && a.X == b.X) return 0;
            else if (a.Y > b.Y || (a.Y == b.Y && a.X > b.X)) return 1;
            else return -1;
        }
    }


    /// <summary>
    /// A Point class for use in an ObservableCollection, which notifies
    /// the event handler whenever a property is changed.
    /// </summary>
    public class OPoint : INotifyPropertyChanged, IEquatable<OPoint> {
        private int x, y;
        public event PropertyChangedEventHandler PropertyChanged;

        public OPoint(int newX, int newY) { x = newX; y = newY; }

        // Notify whenever the X property is changed
        public int X {
            get { return x; }
            set {  x = value;  OnPropertyChanged("X"); }
        }

        // Notify whenever the Y property is changed
        public int Y {
            get { return y; }
            set { y = value; OnPropertyChanged("Y"); }
        }


        // Implement IEquatable<T>.Equals
        public bool Equals(OPoint other) {
            if (X == other.X && Y == other.Y) return true;
            else  return false;
        }


        // Override Object.Equals
        public override bool Equals(Object obj) {
            if (obj == null || !(obj is OPoint)) return false;
            else  return Equals(obj as OPoint);
        }


        // Override Object.GetHashCode
        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode();
        }


        // Raise the event when a property is changed
        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}

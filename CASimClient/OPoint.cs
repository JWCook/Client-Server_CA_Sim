using System;
using System.ComponentModel;


namespace Util {
    /// <summary>
    /// A Point class for use in an ObservableCollection, which notifies
    /// the event handler whenever a property is changed. Used in CALoaderWindow
    /// to display a neighborhood set while it is being made.
    /// </summary>
    public class OPoint : INotifyPropertyChanged, IEquatable<OPoint> {
        private int x, y;
        public event PropertyChangedEventHandler PropertyChanged;

        public OPoint(int newX, int newY) { x = newX; y = newY; }

        // Notify event handler whenever the X property is changed
        public int X {
            get { return x; }
            set { x = value; OnPropertyChanged("X"); }
        }

        // Notify event handler whenever the Y property is changed
        public int Y {
            get { return y; }
            set { y = value; OnPropertyChanged("Y"); }
        }


        // Implement IEquatable<T>.Equals
        public bool Equals(OPoint other) {
            if (X == other.X && Y == other.Y) return true;
            else return false;
        }


        // Override Object.Equals
        public override bool Equals(Object obj) {
            if (obj == null || !(obj is OPoint)) return false;
            else return Equals((OPoint)obj);
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

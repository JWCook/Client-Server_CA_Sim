using CASimService;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Util;

namespace CASimClient {
    /// <summary>
    /// Window used for loading and compiling a CA.
    /// </summary>
    public partial class CALoaderWindow : Window {
        private ObservableCollection<OPoint> neighborhood;
        private CA ca;
        //private GridViewColumnHeader sortColumn;

        public ObservableCollection<OPoint> Neighborhood { get { return neighborhood; } }
        public CA CompiledCA { get { return ca; } }

        
        public CALoaderWindow(CA currentCA) {
            neighborhood = new ObservableCollection<OPoint>();
            neighborhood.Add(new OPoint(0, 0));
            ca = currentCA;
            InitializeComponent();

            // Load values of curernt CA into fields, if available
            if (ca != null) populateFields();
        }


        /// <summary>
        /// Add a new point to the neighborhood set, without duplicates
        /// </summary>
        private void addPoint(object sender, RoutedEventArgs e) {
            int x = (xInputBox.Text == "") ? 0 : int.Parse(xInputBox.Text);
            int y = (yInputBox.Text == "") ? 0 : int.Parse(yInputBox.Text);
            OPoint newPoint = new OPoint(x, y);
            if (!neighborhood.Contains(newPoint)) neighborhood.Add(newPoint);
            xInputBox.Text = yInputBox.Text = "";
        }


        /// <summary>
        /// Attempt to compile delta function code entered. Any compiler
        /// messages are displayed.
        /// </summary>
        private void compile(object sender, RoutedEventArgs e) {
            // Gather information entered
            string deltaStr = deltaInputBox.Text;
            int numStates = (numStatesInputBox.Text == "") ? 1 : int.Parse(numStatesInputBox.Text);
            uint defaultState = (defaultStateInputBox.Text == "") ? 0 : uint.Parse(defaultStateInputBox.Text);
            CPoint[] neighbors = new CPoint[Neighborhood.Count];
            int i = 0;
            foreach (OPoint p in Neighborhood) {
                neighbors[i] = new CPoint(p.X, p.Y);
                i++;
            }

            // Attempt to compile a new CA with the entered information
            ca = new CA(numStates, defaultState, neighbors, deltaStr);
            compileMesseageOutputBox.Text = ca.compileDelta();
        }


        /// <summary>
        /// Open an open file dialog to let the user choose a CA configuration
        /// file, and attempt to load a CA from it.
        /// </summary>
        private void loadFromFile(object sender, RoutedEventArgs e) {
            OpenFileDialog openCADialog = new OpenFileDialog();
            openCADialog.Filter = "Cellular Automata File (.xml)|*.xml|All Files|*.*";

            // Open selected file and parse results
            if (openCADialog.ShowDialog() == true) {
                filenameBox.Text = openCADialog.FileName;
                ca = CASerializer.Deserialize(openCADialog.FileName);

                // If the file was valid, put results back into input fields
                if (ca != null) populateFields();

                // Otherwise, ignore it and display an error message
                else MessageBox.Show("Error: invalid cellular automata file", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /// <summary>
        /// Load the current CA's values back into the input fields
        /// (used when loading a new CA or editing the current one)
        /// </summary>
        private void populateFields() {
            deltaInputBox.Text = ca.DeltaStr;
            numStatesInputBox.Text = ca.NumStates.ToString();
            defaultStateInputBox.Text = ca.DefaultState.ToString();
            Neighborhood.Clear();
            for (int i = 0; i < ca.Neighborhood.Length; i++)
                Neighborhood.Add(new OPoint(ca.Neighborhood[i].X, ca.Neighborhood[i].Y));
        }


        private void remove(object sender, RoutedEventArgs e) {
            neighborhood.Remove((OPoint)pointList.SelectedItem);
        }

        private void back(object sender, RoutedEventArgs e) {
            this.Close();
        }


        // <summary>
        // Sort the list of points in the neighborhood by the column clicked
        // </summary>
        /*private void sortList(object sender, RoutedEventArgs e) {
            GridViewColumnHeader column = (GridViewColumnHeader)sender;
            string header = (String)column.Column.Header;
            ListSortDirection direction = ListSortDirection.Ascending;
            ICollectionView dataView = CollectionViewSource.GetDefaultView(pointList.ItemsSource);
            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(header, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }*/
    }
}

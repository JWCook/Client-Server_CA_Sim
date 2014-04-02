using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApplication1 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private int gridSizeX;
        private int gridSizeY;
        private Panel[,] cells;

        public MainWindow() {
            InitializeComponent();

            gridSizeX = 500;
            gridSizeY = 500;

            for (int i = 0; i < gridSizeX; i++) caGrid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < gridSizeY; i++) caGrid.RowDefinitions.Add(new RowDefinition());


            cells = new Panel[gridSizeX, gridSizeY];
            for (int i = 0; i < gridSizeX; i++) {
                for (int j = 0; j < gridSizeY; j++) {
                    cells[i,j] = new DockPanel();
                    cells[i,j].Background = Brushes.White;
                    Grid.SetColumn(cells[i,j], i);
                    Grid.SetRow(cells[i,j], j);
                    caGrid.Children.Add(cells[i,j]);
                }
            }
        }

        private void caGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            double cellRatioX = caGrid.Width / gridSizeX;
            double cellRatioY = caGrid.Height / gridSizeY;
            Point clickPos = e.GetPosition(caGrid);
            int cellX = System.Convert.ToInt32(Math.Ceiling(clickPos.X / cellRatioX)) - 1;
            int cellY = System.Convert.ToInt32(Math.Ceiling(clickPos.Y / cellRatioY)) - 1;

            outBox.Text = System.String.Concat(cellX, ", ", cellY);

            cells[cellX, cellY].Background = Brushes.Black;
        }

        private void button4_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < 500; i++) {
                if (cells[i, i].Background == Brushes.Black) cells[i, i].Background = Brushes.White;
                else cells[i, i].Background = Brushes.Black;
            }
        }
    }
}

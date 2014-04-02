using System.Windows;

namespace CASimClient {
    /// <summary>
    /// Interaction logic for GridResizeDialog.xaml
    /// </summary>
    public partial class GridResizeDialog : Window {
        public GridResizeDialog() {
            InitializeComponent();
        }

        public int X {
            get {
                if (xInputBox.Text == "") return 1;
                int x = int.Parse(xInputBox.Text);
                return (x < 1) ? 1 : x;
            }
        }
        public int Y {
            get {
                if (yInputBox.Text == "") return 1;
                int y = int.Parse(yInputBox.Text);
                return (y < 1) ? 1 : y;
            }
        }

        private void ok(object sender, RoutedEventArgs e) {
            this.DialogResult = true;
        }
    }
}

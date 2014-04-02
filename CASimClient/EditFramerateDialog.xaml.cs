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
using System.Windows.Shapes;

namespace CASimClient {
    /// <summary>
    /// Interaction logic for EditFramerateDialog.xaml
    /// </summary>
    public partial class EditFramerateDialog : Window {
        private const int upperLimit = 60;
        private const int lowerLimit = 1;
        int previousRefresh;
        public EditFramerateDialog(int currentRefresh, bool currentSkip) {
            InitializeComponent();
            previousRefresh = currentRefresh;

            // Convert from ms/frame to frames/second
            framerateInputBox.Text = (1000/currentRefresh).ToString();
            frameSkipCheckbox.IsChecked = currentSkip;
        }

        public int RefreshRate {
            get {
                if (framerateInputBox.Text == "") return previousRefresh;
                int framerate = int.Parse(framerateInputBox.Text);
                if (framerate < lowerLimit) framerate = lowerLimit;
                if (framerate > upperLimit) framerate = upperLimit;
                // Convert from frames/second to ms/frame
                return (1000/framerate);
            }
        }

        public bool SkipFrames {
            get {
                return (frameSkipCheckbox.IsChecked == null) ? false : (frameSkipCheckbox.IsChecked == true);
            }
        }

        private void ok(object sender, RoutedEventArgs e) {
            this.DialogResult = true;
        }
    }
}

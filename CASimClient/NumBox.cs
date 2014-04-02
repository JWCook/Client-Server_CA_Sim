using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Util {
    /// <summary>
    /// A TextBox that only accepts positive integer values as input.
    /// </summary>
    public class NumBox : TextBox {
        /// <summary>
        /// Override OnPreviewTextInput to only accept integers
        /// </summary>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e) {
            e.Handled = !containsOnlyNumbers(e.Text);
            base.OnPreviewTextInput(e);
        }

        private bool containsOnlyNumbers(string str) {
            foreach (char c in str) if (!(Char.IsNumber(c) || c == '-')) return false;
            return true;
        }
    }
}

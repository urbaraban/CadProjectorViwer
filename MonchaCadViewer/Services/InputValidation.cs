using System.Text.RegularExpressions;
using System.Windows.Input;

namespace CadProjectorViewer.Services
{
    internal static class InputValidation
    {
        public static void NumberPerDotValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9,]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}

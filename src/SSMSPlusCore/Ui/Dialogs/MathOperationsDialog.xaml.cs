using SSMSPlusCore.Ui.Utils;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;

namespace SSMSPlusCore.Ui.Dialogs
{
    public partial class MathOperationsDialog : Window
    {
        public MathOperationsDialog(MathOperationsResult result)
        {
            InitializeComponent();
            DisplayResults(result);
        }

        private void DisplayResults(MathOperationsResult result)
        {
            // Display selection info
            TotalCellsText.Text = result.TotalCells.ToString();
            NumericCellsText.Text = result.NumericCells.ToString();
            NonNumericCellsText.Text = result.NonNumericCells.ToString();

            if (result.HasData && result.NumericCells > 0)
            {
                // Format numbers with thousand separators and appropriate decimal places
                SumText.Text = FormatNumber(result.Sum);
                AverageText.Text = FormatNumber(result.Average);
                MedianText.Text = FormatNumber(result.Median);
                MinText.Text = FormatNumber(result.Min);
                MaxText.Text = FormatNumber(result.Max);
            }
            else
            {
                SumText.Text = "No numeric data";
                AverageText.Text = "No numeric data";
                MedianText.Text = "No numeric data";
                MinText.Text = "No numeric data";
                MaxText.Text = "No numeric data";
            }
        }

        private string FormatNumber(double value)
        {
            // Check if the number is an integer
            if (Math.Abs(value % 1) < 0.0000001)
            {
                return value.ToString("N0", CultureInfo.CurrentCulture);
            }
            else
            {
                // Use up to 6 decimal places, removing trailing zeros
                return value.ToString("0.######", CultureInfo.CurrentCulture);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new EmptyAutomationPeer(this);
        }
    }
}

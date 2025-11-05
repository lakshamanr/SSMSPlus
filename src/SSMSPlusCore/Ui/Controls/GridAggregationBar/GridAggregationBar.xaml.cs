using System;
using System.Windows;
using System.Windows.Controls;
using SSMSPlusCore.Ui.Utils;

namespace SSMSPlusCore.Ui.Controls.GridAggregationBar
{
    public partial class GridAggregationBar : UserControl
    {
        public GridAggregationBar()
        {
            InitializeComponent();
        }

        public void UpdateStatistics(MathOperationsResult result)
        {
            if (result == null || !result.HasData || result.NumericCells == 0)
            {
                ShowEmpty();
                return;
            }

            // Show statistics panel and hide empty message
            StatsPanel.Visibility = Visibility.Visible;
            EmptyMessage.Visibility = Visibility.Collapsed;

            // Update all statistics labels
            MaxLabel.Text = FormatNumber(result.Max);
            MinLabel.Text = FormatNumber(result.Min);
            AvgLabel.Text = FormatNumber(result.Average);
            SumLabel.Text = FormatNumber(result.Sum);
            CountLabel.Text = result.NumericCells.ToString();
            DistinctLabel.Text = result.DistinctCount.ToString();
        }

        private void ShowEmpty()
        {
            StatsPanel.Visibility = Visibility.Collapsed;
            EmptyMessage.Visibility = Visibility.Visible;
        }

        private string FormatNumber(double value)
        {
            // Check if the value is a whole number
            if (Math.Abs(value % 1) < 0.000001)
            {
                return value.ToString("N0"); // No decimal places for whole numbers
            }
            else
            {
                return value.ToString("N2"); // 2 decimal places for decimals
            }
        }
    }
}

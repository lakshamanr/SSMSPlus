using SSMSPlusCore.Ui.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SSMSPlusCore.Ui.Controls.GridAggregationBar
{
    public partial class GridAggregationBar : UserControl
    {
        public GridAggregationBar()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine($"[GridAggregationBar] Constructor called - Instance: {this.GetHashCode()}");
        }

        public void UpdateStatistics(MathOperationsResult result)
        {
            System.Diagnostics.Debug.WriteLine($"[GridAggregationBar] UpdateStatistics called - Instance: {this.GetHashCode()}, Result: {result != null}, HasData: {result?.HasData}, NumericCells: {result?.NumericCells}");
            
            if (result == null || !result.HasData || result.NumericCells == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[GridAggregationBar] Showing empty state");
                ShowEmpty();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[GridAggregationBar] Updating statistics - Max: {result.Max}, Min: {result.Min}, Avg: {result.Average}");
            
            // Hide empty message
            EmptyMessage.Visibility = Visibility.Collapsed;

            // Show and update MAX
            MaxLabel.Visibility = Visibility.Visible;
            MaxValue.Text = FormatNumber(result.Max);

            // Show and update MIN
            MinLabel.Visibility = Visibility.Visible;
            MinValue.Text = FormatNumber(result.Min);

            // Show and update AVG
            AvgLabel.Visibility = Visibility.Visible;
            AvgValue.Text = FormatNumber(result.Average);

            // Show and update MEDIAN
            MedianLabel.Visibility = Visibility.Visible;
            MedianValue.Text = FormatNumber(result.Median);

            // Show and update SUM
            SumLabel.Visibility = Visibility.Visible;
            SumValue.Text = FormatNumber(result.Sum);

            // Show and update COUNT
            CountLabel.Visibility = Visibility.Visible;
            CountValue.Text = result.NumericCells.ToString();

            // Show and update DISTINCT
            DistinctLabel.Visibility = Visibility.Visible;
            DistinctValue.Text = result.DistinctCount.ToString();
        }

        private void ShowEmpty()
        {
            // Hide all aggregate labels
            MaxLabel.Visibility = Visibility.Collapsed;
            MinLabel.Visibility = Visibility.Collapsed;
            AvgLabel.Visibility = Visibility.Collapsed;
            MedianLabel.Visibility = Visibility.Collapsed;
            SumLabel.Visibility = Visibility.Collapsed;
            CountLabel.Visibility = Visibility.Collapsed;
            DistinctLabel.Visibility = Visibility.Collapsed;

            // Show empty message
            EmptyMessage.Visibility = Visibility.Visible;
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
                // Use up to 2 decimal places for display in status bar (more compact)
                return value.ToString("N2", CultureInfo.CurrentCulture);
            }
        }
    }
}
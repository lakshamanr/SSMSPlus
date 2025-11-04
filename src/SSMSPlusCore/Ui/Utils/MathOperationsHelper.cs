using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SSMSPlusCore.Ui.Utils
{
    public class MathOperationsResult
    {
        public int TotalCells { get; set; }
        public int NumericCells { get; set; }
        public int NonNumericCells { get; set; }
        public double Sum { get; set; }
        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Median { get; set; }
        public bool HasData { get; set; }
    }

    public static class MathOperationsHelper
    {
        public static MathOperationsResult CalculateStatistics(DataGrid dataGrid)
        {
            var result = new MathOperationsResult();
            var numericValues = new List<double>();

            // Get selected cells
            var selectedCells = dataGrid.SelectedCells;
            result.TotalCells = selectedCells.Count;

            if (selectedCells.Count == 0)
            {
                result.HasData = false;
                return result;
            }

            // Extract numeric values from selected cells
            foreach (var cellInfo in selectedCells)
            {
                try
                {
                    var cellValue = GetCellValue(cellInfo);
                    if (cellValue != null)
                    {
                        var stringValue = cellValue.ToString().Trim();

                        // Try to parse as double
                        if (double.TryParse(stringValue, out double numericValue))
                        {
                            numericValues.Add(numericValue);
                        }
                    }
                }
                catch
                {
                    // Ignore cells that can't be processed
                }
            }

            result.NumericCells = numericValues.Count;
            result.NonNumericCells = result.TotalCells - result.NumericCells;

            if (numericValues.Count == 0)
            {
                result.HasData = false;
                return result;
            }

            // Calculate statistics
            result.HasData = true;
            result.Sum = numericValues.Sum();
            result.Average = numericValues.Average();
            result.Min = numericValues.Min();
            result.Max = numericValues.Max();
            result.Median = CalculateMedian(numericValues);

            return result;
        }

        private static object GetCellValue(DataGridCellInfo cellInfo)
        {
            if (cellInfo.Column == null || cellInfo.Item == null)
                return null;

            // For DataGridTextColumn
            if (cellInfo.Column is DataGridTextColumn textColumn)
            {
                var binding = textColumn.Binding as System.Windows.Data.Binding;
                if (binding != null && binding.Path != null)
                {
                    var property = cellInfo.Item.GetType().GetProperty(binding.Path.Path);
                    if (property != null)
                    {
                        return property.GetValue(cellInfo.Item);
                    }
                }
            }
            // For DataGridTemplateColumn - try to get ClipboardContentBinding
            else if (cellInfo.Column is DataGridTemplateColumn templateColumn)
            {
                var clipboardBinding = templateColumn.ClipboardContentBinding as System.Windows.Data.Binding;
                if (clipboardBinding != null && clipboardBinding.Path != null)
                {
                    var property = cellInfo.Item.GetType().GetProperty(clipboardBinding.Path.Path);
                    if (property != null)
                    {
                        return property.GetValue(cellInfo.Item);
                    }
                }
            }

            // Fallback: try to get from column's SortMemberPath
            if (!string.IsNullOrEmpty(cellInfo.Column.SortMemberPath))
            {
                var property = cellInfo.Item.GetType().GetProperty(cellInfo.Column.SortMemberPath);
                if (property != null)
                {
                    return property.GetValue(cellInfo.Item);
                }
            }

            return null;
        }

        private static double CalculateMedian(List<double> values)
        {
            if (values.Count == 0)
                return 0;

            var sorted = values.OrderBy(x => x).ToList();
            int count = sorted.Count;

            if (count % 2 == 0)
            {
                // Even number of elements - average of two middle values
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            else
            {
                // Odd number of elements - middle value
                return sorted[count / 2];
            }
        }
    }
}

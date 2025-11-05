using System;
using System.Collections.Generic;
using System.Globalization;
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
        public int DistinctCount { get; set; }
        public bool HasData { get; set; }
    }

    public static class MathOperationsHelper
    {
        public static MathOperationsResult CalculateStatistics(DataGrid dataGrid)
        {
            var result = new MathOperationsResult();

            if (dataGrid == null || dataGrid.SelectedCells.Count == 0)
            {
                result.HasData = false;
                return result;
            }

            var values = new List<double>();
            var distinctValues = new HashSet<string>();
            var totalCells = 0;

            // Extract numeric values from selected cells
            foreach (var selectedCell in dataGrid.SelectedCells)
            {
                totalCells++;

                var cellContent = selectedCell.Column?.OnCopyingCellClipboardContent(selectedCell.Item);
                if (cellContent == null)
                    continue;

                var cellValue = cellContent.ToString();
                if (string.IsNullOrWhiteSpace(cellValue))
                    continue;

                // Track distinct values (case-insensitive)
                distinctValues.Add(cellValue.Trim().ToLowerInvariant());

                // Try to parse as number
                if (TryParseNumber(cellValue, out double numericValue))
                {
                    values.Add(numericValue);
                }
            }

            result.TotalCells = totalCells;
            result.NumericCells = values.Count;
            result.NonNumericCells = totalCells - values.Count;
            result.DistinctCount = distinctValues.Count;
            result.HasData = values.Count > 0;

            if (result.HasData)
            {
                result.Sum = values.Sum();
                result.Average = values.Average();
                result.Min = values.Min();
                result.Max = values.Max();
            }

            return result;
        }

        private static bool TryParseNumber(string value, out double result)
        {
            // Remove common thousand separators
            value = value.Replace(",", "").Replace(" ", "").Trim();

            // Try standard parsing with invariant culture
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return true;

            // Try with current culture
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
                return true;

            result = 0;
            return false;
        }
    }
}

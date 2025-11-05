using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using SSMSPlusCore.Ui.Utils;
using SSMSPlusCore.Ui.Controls.GridAggregationBar;

namespace SSMSPlusCore.Integration.ResultGrid
{
    /// <summary>
    /// Monitors SSMS's native result grid and attaches statistics calculation
    /// </summary>
    public class ResultGridMonitor
    {
        private static ResultGridMonitor _instance;
        private readonly List<WeakReference<DataGrid>> _monitoredGrids = new List<WeakReference<DataGrid>>();
        private GridAggregationBar _statisticsBar;
        private Window _statisticsWindow;

        public static ResultGridMonitor Instance => _instance ?? (_instance = new ResultGridMonitor());

        private ResultGridMonitor()
        {
        }

        /// <summary>
        /// Start monitoring for result grids after a query execution
        /// </summary>
        public void MonitorActiveWindow()
        {
            // Delay to allow result grid to be created
            Task.Delay(500).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    TryFindAndHookResultGrid();
                });
            });
        }

        private void TryFindAndHookResultGrid()
        {
            try
            {
                // Find the main SSMS window
                Window mainWindow = Application.Current?.MainWindow;
                if (mainWindow == null)
                {
                    // Try to find any window
                    mainWindow = Application.Current?.Windows.OfType<Window>().FirstOrDefault();
                }

                if (mainWindow == null)
                    return;

                // Search for DataGrid controls in the visual tree
                var dataGrids = FindVisualChildren<DataGrid>(mainWindow);

                foreach (var grid in dataGrids)
                {
                    // Check if we're already monitoring this grid
                    if (IsAlreadyMonitored(grid))
                        continue;

                    // Check if this looks like a result grid
                    if (IsResultGrid(grid))
                    {
                        HookIntoGrid(grid);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error silently - don't break SSMS
                System.Diagnostics.Debug.WriteLine($"ResultGridMonitor error: {ex.Message}");
            }
        }

        private bool IsResultGrid(DataGrid grid)
        {
            // Heuristics to identify SSMS result grid:
            // 1. Should have auto-generated columns or be in read-only mode
            // 2. Should be part of a document/result pane
            // 3. Should not be part of our own tool windows

            if (grid.IsReadOnly || grid.AutoGenerateColumns)
                return true;

            // Check parent containers for result-related names
            DependencyObject parent = grid;
            while (parent != null)
            {
                if (parent is FrameworkElement element)
                {
                    var name = element.Name?.ToLower() ?? "";
                    if (name.Contains("result") || name.Contains("grid") || name.Contains("query"))
                        return true;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }

        private bool IsAlreadyMonitored(DataGrid grid)
        {
            // Clean up dead references
            _monitoredGrids.RemoveAll(wr =>
            {
                DataGrid g;
                return !wr.TryGetTarget(out g);
            });

            // Check if this grid is already monitored
            foreach (var wr in _monitoredGrids)
            {
                DataGrid monitoredGrid;
                if (wr.TryGetTarget(out monitoredGrid) && monitoredGrid == grid)
                    return true;
            }

            return false;
        }

        private void HookIntoGrid(DataGrid grid)
        {
            try
            {
                // Store weak reference to avoid memory leaks
                _monitoredGrids.Add(new WeakReference<DataGrid>(grid));

                // Enable cell selection
                grid.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;

                // Attach event handlers
                grid.SelectedCellsChanged += Grid_SelectedCellsChanged;
                grid.CurrentCellChanged += Grid_CurrentCellChanged;

                System.Diagnostics.Debug.WriteLine("Successfully hooked into result grid");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to hook grid: {ex.Message}");
            }
        }

        private void Grid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            UpdateStatistics(sender as DataGrid);
        }

        private void Grid_CurrentCellChanged(object sender, EventArgs e)
        {
            UpdateStatistics(sender as DataGrid);
        }

        private void UpdateStatistics(DataGrid grid)
        {
            if (grid == null)
                return;

            try
            {
                // Calculate statistics from selected cells
                var result = MathOperationsHelper.CalculateStatistics(grid);

                // Show statistics in floating window
                ShowStatisticsWindow(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Statistics calculation error: {ex.Message}");
            }
        }

        private void ShowStatisticsWindow(MathOperationsResult result)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Create statistics window if it doesn't exist
                    if (_statisticsWindow == null)
                    {
                        _statisticsBar = new GridAggregationBar();

                        _statisticsWindow = new Window
                        {
                            Title = "Data Statistics",
                            Content = _statisticsBar,
                            Width = 800,
                            Height = 50,
                            WindowStyle = WindowStyle.ToolWindow,
                            ResizeMode = ResizeMode.NoResize,
                            Topmost = true,
                            ShowInTaskbar = false
                        };

                        // Position at bottom of screen
                        _statisticsWindow.Left = (SystemParameters.PrimaryScreenWidth - 800) / 2;
                        _statisticsWindow.Top = SystemParameters.PrimaryScreenHeight - 150;
                    }

                    // Update statistics
                    _statisticsBar.UpdateStatistics(result);

                    // Show window if it has data
                    if (result.HasData && result.NumericCells > 0)
                    {
                        if (!_statisticsWindow.IsVisible)
                            _statisticsWindow.Show();
                    }
                    else
                    {
                        if (_statisticsWindow.IsVisible)
                            _statisticsWindow.Hide();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Statistics window error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Find all visual children of a specific type
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current is T typedCurrent)
                {
                    yield return typedCurrent;
                }

                int childCount = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child != null)
                        queue.Enqueue(child);
                }
            }
        }

        public void Cleanup()
        {
            _statisticsWindow?.Close();
            _statisticsWindow = null;
            _monitoredGrids.Clear();
        }
    }
}

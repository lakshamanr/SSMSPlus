using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
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

        // Win32 API imports for window enumeration
        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, StringBuilder lParam);

        private const uint WM_GETTEXT = 0x000D;
        private const uint WM_GETTEXTLENGTH = 0x000E;

        private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        public static ResultGridMonitor Instance => _instance ?? (_instance = new ResultGridMonitor());

        private ResultGridMonitor()
        {
        }

        /// <summary>
        /// Start monitoring for result grids after a query execution
        /// </summary>
        public void MonitorActiveWindow()
        {
            LogDebug("MonitorActiveWindow called");

            // Try multiple delays to catch the grid at different times
            Task.Delay(500).ContinueWith(_ => TryFindAndHookResultGridSafe());
            Task.Delay(1500).ContinueWith(_ => TryFindAndHookResultGridSafe());
            Task.Delay(3000).ContinueWith(_ => TryFindAndHookResultGridSafe());
        }

        private void TryFindAndHookResultGridSafe()
        {
            try
            {
                // Try to get dispatcher
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Invoke(() => TryFindAndHookResultGrid());
                }
                else
                {
                    // Try without dispatcher
                    TryFindAndHookResultGrid();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error in TryFindAndHookResultGridSafe: {ex.Message}");
            }
        }

        private void TryFindAndHookResultGrid()
        {
            try
            {
                LogDebug("TryFindAndHookResultGrid started");

                var foundGrids = new List<DataGrid>();

                // Approach 1: Try Application.Current
                try
                {
                    if (Application.Current != null)
                    {
                        LogDebug("Application.Current found");

                        var windows = Application.Current.Windows.OfType<Window>().ToList();
                        LogDebug($"Found {windows.Count} windows via Application.Current");

                        foreach (var window in windows)
                        {
                            LogDebug($"Searching window: {window.GetType().Name} - {window.Title}");
                            var grids = FindVisualChildren<DataGrid>(window).ToList();
                            LogDebug($"  Found {grids.Count} DataGrids in this window");
                            foundGrids.AddRange(grids);
                        }
                    }
                    else
                    {
                        LogDebug("Application.Current is null");
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"Approach 1 failed: {ex.Message}");
                }

                // Approach 2: Enumerate all HwndSource objects (WPF windows)
                try
                {
                    LogDebug("Trying Approach 2: Enumerate HwndSource");
                    var sources = PresentationSource.CurrentSources.OfType<HwndSource>().ToList();
                    LogDebug($"Found {sources.Count} HwndSource objects");

                    foreach (var source in sources)
                    {
                        if (source.RootVisual is DependencyObject root)
                        {
                            var grids = FindVisualChildren<DataGrid>(root).ToList();
                            LogDebug($"Found {grids.Count} DataGrids in HwndSource");
                            foundGrids.AddRange(grids);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"Approach 2 failed: {ex.Message}");
                }

                LogDebug($"Total DataGrids found: {foundGrids.Count}");

                // Try to hook into found grids
                int hookedCount = 0;
                foreach (var grid in foundGrids.Distinct())
                {
                    try
                    {
                        // Check if we're already monitoring this grid
                        if (IsAlreadyMonitored(grid))
                        {
                            LogDebug($"Grid already monitored: {grid.GetHashCode()}");
                            continue;
                        }

                        // Check if this looks like a result grid
                        if (IsResultGrid(grid))
                        {
                            LogDebug($"Hooking into grid: {grid.GetHashCode()}, Items: {grid.Items.Count}, Columns: {grid.Columns.Count}");
                            HookIntoGrid(grid);
                            hookedCount++;
                        }
                        else
                        {
                            LogDebug($"Grid doesn't look like result grid: {grid.GetHashCode()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error checking/hooking grid: {ex.Message}");
                    }
                }

                LogDebug($"Successfully hooked {hookedCount} grids");
            }
            catch (Exception ex)
            {
                // Log error silently - don't break SSMS
                LogDebug($"ResultGridMonitor error: {ex.Message}\n{ex.StackTrace}");
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
            LogDebug($"Grid_SelectedCellsChanged event fired. Sender: {sender?.GetType().Name}");
            UpdateStatistics(sender as DataGrid);
        }

        private void Grid_CurrentCellChanged(object sender, EventArgs e)
        {
            LogDebug($"Grid_CurrentCellChanged event fired. Sender: {sender?.GetType().Name}");
            UpdateStatistics(sender as DataGrid);
        }

        private void UpdateStatistics(DataGrid grid)
        {
            if (grid == null)
            {
                LogDebug("UpdateStatistics: grid is null");
                return;
            }

            try
            {
                LogDebug($"UpdateStatistics: Grid has {grid.SelectedCells.Count} selected cells");

                // Calculate statistics from selected cells
                var result = MathOperationsHelper.CalculateStatistics(grid);

                LogDebug($"Statistics calculated: HasData={result.HasData}, NumericCells={result.NumericCells}");

                // Show statistics in floating window
                ShowStatisticsWindow(result);
            }
            catch (Exception ex)
            {
                LogDebug($"Statistics calculation error: {ex.Message}");
            }
        }

        private void ShowStatisticsWindow(MathOperationsResult result)
        {
            try
            {
                LogDebug("ShowStatisticsWindow called");

                // Try to get the current dispatcher
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                if (dispatcher != null)
                {
                    dispatcher.Invoke(() => ShowStatisticsWindowCore(result));
                }
                else
                {
                    // Try without dispatcher
                    ShowStatisticsWindowCore(result);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"ShowStatisticsWindow error: {ex.Message}");
            }
        }

        private void ShowStatisticsWindowCore(MathOperationsResult result)
        {
            try
            {
                // Create statistics window if it doesn't exist
                if (_statisticsWindow == null)
                {
                    LogDebug("Creating new statistics window");

                    _statisticsBar = new GridAggregationBar();

                    _statisticsWindow = new Window
                    {
                        Title = "Data Statistics - SSMS Plus",
                        Content = _statisticsBar,
                        Width = 800,
                        Height = 60,
                        WindowStyle = WindowStyle.ToolWindow,
                        ResizeMode = ResizeMode.CanResize,
                        Topmost = true,
                        ShowInTaskbar = false
                    };

                    // Position at bottom of screen
                    _statisticsWindow.Left = (SystemParameters.PrimaryScreenWidth - 800) / 2;
                    _statisticsWindow.Top = SystemParameters.PrimaryScreenHeight - 150;

                    LogDebug($"Window created at position ({_statisticsWindow.Left}, {_statisticsWindow.Top})");
                }

                // Update statistics
                _statisticsBar.UpdateStatistics(result);
                LogDebug("Statistics bar updated");

                // Show window if it has data
                if (result.HasData && result.NumericCells > 0)
                {
                    if (!_statisticsWindow.IsVisible)
                    {
                        _statisticsWindow.Show();
                        LogDebug("Statistics window shown");
                    }
                }
                else
                {
                    if (_statisticsWindow.IsVisible)
                    {
                        _statisticsWindow.Hide();
                        LogDebug("Statistics window hidden (no data)");
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Statistics window error: {ex.Message}\n{ex.StackTrace}");
            }
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

        /// <summary>
        /// Log debug messages to both Debug output and log file
        /// </summary>
        private void LogDebug(string message)
        {
            try
            {
                var fullMessage = $"[ResultGridMonitor] {DateTime.Now:HH:mm:ss.fff} - {message}";

                // Output to Debug window
                System.Diagnostics.Debug.WriteLine(fullMessage);

                // Also write to log file
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SSMS Plus",
                    "ResultGridMonitor.log");

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                System.IO.File.AppendAllText(logPath, fullMessage + Environment.NewLine);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        /// <summary>
        /// Public method to manually trigger grid search (for testing)
        /// </summary>
        public void ManualScan()
        {
            LogDebug("=== MANUAL SCAN TRIGGERED ===");
            TryFindAndHookResultGrid();
        }
    }
}

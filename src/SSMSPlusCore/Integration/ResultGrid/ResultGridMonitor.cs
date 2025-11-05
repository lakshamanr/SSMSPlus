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
using System.Windows.Automation;
using System.Windows.Forms.Integration;
using SSMSPlusCore.Ui.Utils;
using SSMSPlusCore.Ui.Controls.GridAggregationBar;
using WinForms = System.Windows.Forms;

namespace SSMSPlusCore.Integration.ResultGrid
{
    /// <summary>
    /// Monitors SSMS's native result grid (Windows Forms or WPF) and shows statistics
    /// Implements the same functionality as dbForge SQL Complete
    /// </summary>
    public class ResultGridMonitor
    {
        private static ResultGridMonitor _instance;
        private readonly List<WeakReference<DataGrid>> _monitoredWpfGrids = new List<WeakReference<DataGrid>>();
        private readonly List<WeakReference<WinForms.DataGridView>> _monitoredWinFormsGrids = new List<WeakReference<WinForms.DataGridView>>();
        private GridAggregationBar _statisticsBar;
        private Window _statisticsWindow;
        private System.Timers.Timer _scanTimer;

        public static ResultGridMonitor Instance => _instance ?? (_instance = new ResultGridMonitor());

        private ResultGridMonitor()
        {
            // Create a timer to periodically scan for new grids
            _scanTimer = new System.Timers.Timer(2000); // Every 2 seconds
            _scanTimer.Elapsed += (s, e) => TryFindAndHookResultGridSafe();
            _scanTimer.AutoReset = true;
        }

        /// <summary>
        /// Start monitoring for result grids after a query execution
        /// </summary>
        public void MonitorActiveWindow()
        {
            LogDebug("MonitorActiveWindow called");

            // Start the periodic scanning
            if (!_scanTimer.Enabled)
            {
                _scanTimer.Start();
                LogDebug("Started periodic grid scanning");
            }

            // Also do immediate scans at different delays
            Task.Delay(300).ContinueWith(_ => TryFindAndHookResultGridSafe());
            Task.Delay(1000).ContinueWith(_ => TryFindAndHookResultGridSafe());
            Task.Delay(2500).ContinueWith(_ => TryFindAndHookResultGridSafe());
        }

        private void TryFindAndHookResultGridSafe()
        {
            try
            {
                // Try to get UI thread dispatcher
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    TryFindAndHookResultGrid();
                }));
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
                LogDebug("=== Starting Grid Search ===");

                int foundWpf = 0, foundWinForms = 0, hookedWpf = 0, hookedWinForms = 0;

                // Approach 1: Search for WPF DataGrid controls
                try
                {
                    var wpfGrids = FindWpfDataGrids();
                    foundWpf = wpfGrids.Count;
                    LogDebug($"Found {foundWpf} WPF DataGrids");

                    foreach (var grid in wpfGrids)
                    {
                        if (!IsAlreadyMonitoredWpf(grid) && IsResultGrid(grid))
                        {
                            HookIntoWpfGrid(grid);
                            hookedWpf++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"WPF grid search error: {ex.Message}");
                }

                // Approach 2: Search for Windows Forms DataGridView controls
                try
                {
                    var winFormsGrids = FindWinFormsDataGridViews();
                    foundWinForms = winFormsGrids.Count;
                    LogDebug($"Found {foundWinForms} WinForms DataGridViews");

                    foreach (var grid in winFormsGrids)
                    {
                        if (!IsAlreadyMonitoredWinForms(grid) && IsResultGridWinForms(grid))
                        {
                            HookIntoWinFormsGrid(grid);
                            hookedWinForms++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"WinForms grid search error: {ex.Message}");
                }

                // Approach 3: Use UI Automation to find any grid
                try
                {
                    FindGridsViaUIAutomation();
                }
                catch (Exception ex)
                {
                    LogDebug($"UI Automation search error: {ex.Message}");
                }

                LogDebug($"=== Search Complete: Found {foundWpf} WPF + {foundWinForms} WinForms, Hooked {hookedWpf} WPF + {hookedWinForms} WinForms ===");
            }
            catch (Exception ex)
            {
                LogDebug($"TryFindAndHookResultGrid error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #region WPF DataGrid Handling

        private List<DataGrid> FindWpfDataGrids()
        {
            var grids = new List<DataGrid>();

            // Try Application.Current windows
            if (Application.Current != null)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    try
                    {
                        grids.AddRange(FindVisualChildren<DataGrid>(window));
                    }
                    catch { }
                }
            }

            // Try HwndSource
            foreach (var source in PresentationSource.CurrentSources.OfType<HwndSource>())
            {
                try
                {
                    if (source.RootVisual is DependencyObject root)
                    {
                        grids.AddRange(FindVisualChildren<DataGrid>(root));
                    }
                }
                catch { }
            }

            return grids.Distinct().ToList();
        }

        private bool IsResultGrid(DataGrid grid)
        {
            try
            {
                // Check if it has data and looks like a result grid
                if (grid.Items.Count > 0 || grid.Columns.Count > 0)
                {
                    // Check if read-only or auto-generated
                    if (grid.IsReadOnly || grid.AutoGenerateColumns)
                        return true;

                    // Check parent hierarchy for result-related names
                    DependencyObject parent = grid;
                    int depth = 0;
                    while (parent != null && depth < 10)
                    {
                        if (parent is FrameworkElement element)
                        {
                            var name = element.Name?.ToLower() ?? "";
                            var type = element.GetType().Name.ToLower();
                            if (name.Contains("result") || name.Contains("grid") || name.Contains("query") ||
                                type.Contains("result") || type.Contains("grid"))
                                return true;
                        }
                        parent = VisualTreeHelper.GetParent(parent);
                        depth++;
                    }
                }
            }
            catch { }

            return false;
        }

        private bool IsAlreadyMonitoredWpf(DataGrid grid)
        {
            _monitoredWpfGrids.RemoveAll(wr => !wr.TryGetTarget(out _));

            foreach (var wr in _monitoredWpfGrids)
            {
                if (wr.TryGetTarget(out DataGrid monitoredGrid) && monitoredGrid == grid)
                    return true;
            }
            return false;
        }

        private void HookIntoWpfGrid(DataGrid grid)
        {
            try
            {
                LogDebug($"Hooking WPF grid: Items={grid.Items.Count}, Columns={grid.Columns.Count}");

                _monitoredWpfGrids.Add(new WeakReference<DataGrid>(grid));

                grid.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
                grid.SelectedCellsChanged += WpfGrid_SelectedCellsChanged;
                grid.CurrentCellChanged += WpfGrid_CurrentCellChanged;

                LogDebug("Successfully hooked WPF grid");
            }
            catch (Exception ex)
            {
                LogDebug($"Failed to hook WPF grid: {ex.Message}");
            }
        }

        private void WpfGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            LogDebug("WPF Grid selection changed");
            UpdateStatisticsFromWpfGrid(sender as DataGrid);
        }

        private void WpfGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            LogDebug("WPF Grid current cell changed");
            UpdateStatisticsFromWpfGrid(sender as DataGrid);
        }

        private void UpdateStatisticsFromWpfGrid(DataGrid grid)
        {
            if (grid == null) return;

            try
            {
                LogDebug($"Calculating statistics from WPF grid: {grid.SelectedCells.Count} selected cells");
                var result = MathOperationsHelper.CalculateStatistics(grid);
                ShowStatisticsWindow(result);
            }
            catch (Exception ex)
            {
                LogDebug($"WPF statistics error: {ex.Message}");
            }
        }

        #endregion

        #region Windows Forms DataGridView Handling

        private List<WinForms.DataGridView> FindWinFormsDataGridViews()
        {
            var grids = new List<WinForms.DataGridView>();

            try
            {
                // Get all Windows Forms in the application
                foreach (WinForms.Form form in WinForms.Application.OpenForms)
                {
                    try
                    {
                        FindWinFormsControlsRecursive(form, grids);
                    }
                    catch { }
                }

                // Also check WindowsFormsHost controls in WPF
                if (Application.Current != null)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        try
                        {
                            var hosts = FindVisualChildren<WindowsFormsHost>(window);
                            foreach (var host in hosts)
                            {
                                if (host.Child is WinForms.Control control)
                                {
                                    FindWinFormsControlsRecursive(control, grids);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"FindWinFormsDataGridViews error: {ex.Message}");
            }

            return grids;
        }

        private void FindWinFormsControlsRecursive(WinForms.Control parent, List<WinForms.DataGridView> grids)
        {
            try
            {
                if (parent is WinForms.DataGridView grid)
                {
                    grids.Add(grid);
                }

                foreach (WinForms.Control child in parent.Controls)
                {
                    FindWinFormsControlsRecursive(child, grids);
                }
            }
            catch { }
        }

        private bool IsResultGridWinForms(WinForms.DataGridView grid)
        {
            try
            {
                // Check if it has data
                if (grid.Rows.Count > 0 || grid.Columns.Count > 0)
                {
                    // Check if read-only
                    if (grid.ReadOnly)
                        return true;

                    // Check name
                    var name = grid.Name?.ToLower() ?? "";
                    if (name.Contains("result") || name.Contains("grid") || name.Contains("query"))
                        return true;

                    // If it has data and is in a visible form, assume it's a result grid
                    if (grid.Visible && grid.Rows.Count > 0)
                        return true;
                }
            }
            catch { }

            return false;
        }

        private bool IsAlreadyMonitoredWinForms(WinForms.DataGridView grid)
        {
            _monitoredWinFormsGrids.RemoveAll(wr => !wr.TryGetTarget(out _));

            foreach (var wr in _monitoredWinFormsGrids)
            {
                if (wr.TryGetTarget(out WinForms.DataGridView monitoredGrid) && monitoredGrid == grid)
                    return true;
            }
            return false;
        }

        private void HookIntoWinFormsGrid(WinForms.DataGridView grid)
        {
            try
            {
                LogDebug($"Hooking WinForms grid: Rows={grid.Rows.Count}, Columns={grid.Columns.Count}, Name={grid.Name}");

                _monitoredWinFormsGrids.Add(new WeakReference<WinForms.DataGridView>(grid));

                grid.SelectionMode = WinForms.DataGridViewSelectionMode.CellSelect;
                grid.SelectionChanged += WinFormsGrid_SelectionChanged;
                grid.CurrentCellChanged += WinFormsGrid_CurrentCellChanged;

                LogDebug("Successfully hooked WinForms grid");
            }
            catch (Exception ex)
            {
                LogDebug($"Failed to hook WinForms grid: {ex.Message}");
            }
        }

        private void WinFormsGrid_SelectionChanged(object sender, EventArgs e)
        {
            LogDebug("WinForms Grid selection changed");
            UpdateStatisticsFromWinFormsGrid(sender as WinForms.DataGridView);
        }

        private void WinFormsGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            LogDebug("WinForms Grid current cell changed");
            UpdateStatisticsFromWinFormsGrid(sender as WinForms.DataGridView);
        }

        private void UpdateStatisticsFromWinFormsGrid(WinForms.DataGridView grid)
        {
            if (grid == null) return;

            try
            {
                var selectedCells = grid.SelectedCells;
                LogDebug($"Calculating statistics from WinForms grid: {selectedCells.Count} selected cells");

                var values = new List<double>();
                var distinctValues = new HashSet<string>();

                foreach (WinForms.DataGridViewCell cell in selectedCells)
                {
                    try
                    {
                        var cellValue = cell.Value?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            distinctValues.Add(cellValue.Trim().ToLowerInvariant());

                            if (TryParseNumber(cellValue, out double numericValue))
                            {
                                values.Add(numericValue);
                            }
                        }
                    }
                    catch { }
                }

                var result = new MathOperationsResult
                {
                    TotalCells = selectedCells.Count,
                    NumericCells = values.Count,
                    NonNumericCells = selectedCells.Count - values.Count,
                    DistinctCount = distinctValues.Count,
                    HasData = values.Count > 0
                };

                if (values.Count > 0)
                {
                    result.Sum = values.Sum();
                    result.Average = values.Average();
                    result.Min = values.Min();
                    result.Max = values.Max();
                }

                ShowStatisticsWindow(result);
            }
            catch (Exception ex)
            {
                LogDebug($"WinForms statistics error: {ex.Message}");
            }
        }

        private bool TryParseNumber(string value, out double result)
        {
            value = value.Replace(",", "").Replace(" ", "").Trim();

            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out result))
                return true;

            if (double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out result))
                return true;

            result = 0;
            return false;
        }

        #endregion

        #region UI Automation Approach

        private void FindGridsViaUIAutomation()
        {
            try
            {
                LogDebug("Trying UI Automation approach");

                // Get the current process
                var currentProcess = Process.GetCurrentProcess();
                var mainWindow = AutomationElement.FromHandle(currentProcess.MainWindowHandle);

                if (mainWindow == null)
                {
                    LogDebug("Could not get main window via UI Automation");
                    return;
                }

                LogDebug($"Main window found: {mainWindow.Current.Name}");

                // First, search for DataGrid or Table patterns
                var gridCondition = new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataGrid),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Table)
                );

                var grids = mainWindow.FindAll(TreeScope.Descendants, gridCondition);
                LogDebug($"Found {grids.Count} grids via UI Automation (DataGrid/Table)");

                foreach (AutomationElement grid in grids)
                {
                    try
                    {
                        LogDebug($"  Grid: Name={grid.Current.Name}, ClassName={grid.Current.ClassName}, ControlType={grid.Current.ControlType.ProgrammaticName}");
                    }
                    catch { }
                }

                // EXPLORATORY: Search for ALL control types to see what's in the window
                LogDebug("=== EXPLORATORY SEARCH: Finding all major control types ===");

                var controlTypes = new[]
                {
                    ControlType.DataGrid,
                    ControlType.Table,
                    ControlType.List,
                    ControlType.Tree,
                    ControlType.Custom,
                    ControlType.Pane,
                    ControlType.Document,
                    ControlType.Window
                };

                foreach (var controlType in controlTypes)
                {
                    try
                    {
                        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, controlType);
                        var elements = mainWindow.FindAll(TreeScope.Descendants, condition);

                        if (elements.Count > 0)
                        {
                            LogDebug($"Found {elements.Count} {controlType.ProgrammaticName} controls:");

                            int maxLog = Math.Min(5, elements.Count); // Log first 5 of each type
                            for (int i = 0; i < maxLog; i++)
                            {
                                var element = elements[i];
                                try
                                {
                                    var name = element.Current.Name;
                                    var className = element.Current.ClassName;
                                    var automationId = element.Current.AutomationId;

                                    LogDebug($"  [{i}] Name='{name}', Class='{className}', AutomationId='{automationId}'");

                                    // Check if this looks like a result grid based on name/id
                                    if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(automationId))
                                    {
                                        var nameL = (name ?? "").ToLower();
                                        var idL = (automationId ?? "").ToLower();
                                        var classL = (className ?? "").ToLower();

                                        if (nameL.Contains("result") || nameL.Contains("grid") || nameL.Contains("query") ||
                                            idL.Contains("result") || idL.Contains("grid") || idL.Contains("query") ||
                                            classL.Contains("result") || classL.Contains("grid") || classL.Contains("query"))
                                        {
                                            LogDebug($"    ^^^ POTENTIAL RESULT GRID! ^^^");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogDebug($"  [{i}] Error reading element: {ex.Message}");
                                }
                            }

                            if (elements.Count > maxLog)
                            {
                                LogDebug($"  ... and {elements.Count - maxLog} more");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error searching for {controlType.ProgrammaticName}: {ex.Message}");
                    }
                }

                LogDebug("=== END EXPLORATORY SEARCH ===");
            }
            catch (Exception ex)
            {
                LogDebug($"UI Automation error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region Statistics Window

        private void ShowStatisticsWindow(MathOperationsResult result)
        {
            try
            {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                {
                    ShowStatisticsWindowCore(result);
                }));
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
                if (_statisticsWindow == null)
                {
                    LogDebug("Creating statistics window");

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

                    _statisticsWindow.Left = (SystemParameters.PrimaryScreenWidth - 800) / 2;
                    _statisticsWindow.Top = SystemParameters.PrimaryScreenHeight - 150;

                    LogDebug("Statistics window created");
                }

                _statisticsBar.UpdateStatistics(result);

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
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"ShowStatisticsWindowCore error: {ex.Message}");
            }
        }

        #endregion

        #region Helpers

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current is T typedCurrent)
                    yield return typedCurrent;

                try
                {
                    int childCount = VisualTreeHelper.GetChildrenCount(current);
                    for (int i = 0; i < childCount; i++)
                    {
                        var child = VisualTreeHelper.GetChild(current, i);
                        if (child != null)
                            queue.Enqueue(child);
                    }
                }
                catch { }
            }
        }

        public void Cleanup()
        {
            _scanTimer?.Stop();
            _statisticsWindow?.Close();
            _statisticsWindow = null;
            _monitoredWpfGrids.Clear();
            _monitoredWinFormsGrids.Clear();
        }

        private void LogDebug(string message)
        {
            try
            {
                var fullMessage = $"[ResultGridMonitor] {DateTime.Now:HH:mm:ss.fff} - {message}";
                System.Diagnostics.Debug.WriteLine(fullMessage);

                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SSMS Plus",
                    "ResultGridMonitor.log");

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                System.IO.File.AppendAllText(logPath, fullMessage + Environment.NewLine);
            }
            catch { }
        }

        public void ManualScan()
        {
            LogDebug("=== MANUAL SCAN TRIGGERED ===");
            TryFindAndHookResultGrid();
        }

        #endregion
    }
}

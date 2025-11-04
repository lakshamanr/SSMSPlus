using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SSMSPlusCore.Ui.Utils;
using SSMSPlusCore.Ui.Dialogs;
using SSMSPlusCore.Ui.Controls.GridAggregationBar;

namespace SSMSPlusCore.Ui.Extensions
{
    public class EnhancedDataGrid : DataGrid
    {
        private List<SortDescription> _sortDescriptions = new List<SortDescription>();

        public static readonly DependencyProperty AggregationBarProperty =
            DependencyProperty.RegisterAttached(
                "AggregationBar",
                typeof(GridAggregationBar),
                typeof(EnhancedDataGrid),
                new PropertyMetadata(null, OnAggregationBarChanged));

        public static void SetAggregationBar(DependencyObject element, GridAggregationBar value)
        {
            element.SetValue(AggregationBarProperty, value);
        }

        public static GridAggregationBar GetAggregationBar(DependencyObject element)
        {
            return (GridAggregationBar)element.GetValue(AggregationBarProperty);
        }

        private static void OnAggregationBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] OnAggregationBarChanged called - OldValue: {e.OldValue != null}, NewValue: {e.NewValue != null}");

            if (d is EnhancedDataGrid grid)
            {
                System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] Grid instance found");

                // Always unsubscribe old handler
                grid.SelectionChanged -= Grid_SelectionChanged;

                if (e.NewValue != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] Subscribing to SelectionChanged");
                    grid.SelectionChanged += Grid_SelectionChanged;

                    // IMPROVEMENT: Trigger initial update
                    var aggregationBar = e.NewValue as GridAggregationBar;
                    if (aggregationBar != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] AggregationBar is valid, calculating initial statistics");
                        var result = MathOperationsHelper.CalculateStatistics(grid);
                        aggregationBar.UpdateStatistics(result);
                    }
                }
                else if (e.OldValue is GridAggregationBar oldBar)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] Clearing old AggregationBar");
                    // IMPROVEMENT: Clear old bar when detached
                    oldBar.UpdateStatistics(null);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] ERROR: DependencyObject is not EnhancedDataGrid, it's {d?.GetType().Name}");
            }
        }

        private static void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is EnhancedDataGrid grid)
            {
                var aggregationBar = GetAggregationBar(grid);
                // Add a unique prefix to identify YOUR code
                System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] SelectionChanged - AggregationBar: {aggregationBar != null}");
                grid.Dispatcher.InvokeAsync(() =>
                {
                    System.Diagnostics.Debug.WriteLine($"[EnhancedDataGrid] Calculating statistics...");
                    var result = MathOperationsHelper.CalculateStatistics(grid);
                    aggregationBar.UpdateStatistics(result);
                });
            }
        }

        public EnhancedDataGrid()
        {
            // Enable extended selection mode to allow cell selection
            this.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
        }

        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            base.OnSorting(eventArgs);
            SaveSorting();
        }
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            RestoreSorting(newValue);
        }

        public void ShowMathOperations()
        {
            var result = MathOperationsHelper.CalculateStatistics(this);
            var dialog = new MathOperationsDialog(result);
            dialog.Owner = System.Windows.Window.GetWindow(this);
            dialog.ShowDialog();
        }

        private void RestoreSorting(IEnumerable newItemSource)
        {
            if (newItemSource == null)
                return;

            ICollectionView view = CollectionViewSource.GetDefaultView(newItemSource);
            view.SortDescriptions.Clear();

            foreach (SortDescription sortDescription in _sortDescriptions)
            {
                view.SortDescriptions.Add(sortDescription);

                DataGridColumn column = Columns.FirstOrDefault(c => c.SortMemberPath == sortDescription.PropertyName);
                if (column != null)
                    column.SortDirection = sortDescription.Direction;
            }
        }

        private void SaveSorting()
        {
            if (ItemsSource == null)
                return;

            ICollectionView view = CollectionViewSource.GetDefaultView(ItemsSource);
            _sortDescriptions = new List<SortDescription>(view.SortDescriptions);
        }
    }
}

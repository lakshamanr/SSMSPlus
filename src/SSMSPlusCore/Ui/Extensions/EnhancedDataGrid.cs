using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SSMSPlusCore.Ui.Controls.GridAggregationBar;
using SSMSPlusCore.Ui.Utils;

namespace SSMSPlusCore.Ui.Extensions
{
    public class EnhancedDataGrid : DataGrid
    {
        private List<SortDescription> _sortDescriptions = new List<SortDescription>();

        // Attached property for aggregation bar
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
            if (!(d is DataGrid grid))
                return;

            var oldBar = e.OldValue as GridAggregationBar;
            var newBar = e.NewValue as GridAggregationBar;

            // Unsubscribe from old events
            if (oldBar != null)
            {
                grid.SelectedCellsChanged -= Grid_SelectedCellsChanged;
                grid.CurrentCellChanged -= Grid_CurrentCellChanged;
            }

            // Subscribe to new events
            if (newBar != null)
            {
                grid.SelectedCellsChanged += Grid_SelectedCellsChanged;
                grid.CurrentCellChanged += Grid_CurrentCellChanged;

                // Enable cell selection mode
                grid.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;

                // Trigger initial calculation if there are selected cells
                if (grid.SelectedCells.Count > 0)
                {
                    UpdateAggregationBar(grid, newBar);
                }
            }
        }

        private static void Grid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            var aggregationBar = GetAggregationBar(grid);

            if (grid != null && aggregationBar != null)
            {
                UpdateAggregationBar(grid, aggregationBar);
            }
        }

        private static void Grid_CurrentCellChanged(object sender, System.EventArgs e)
        {
            var grid = sender as DataGrid;
            var aggregationBar = GetAggregationBar(grid);

            if (grid != null && aggregationBar != null)
            {
                UpdateAggregationBar(grid, aggregationBar);
            }
        }

        private static void UpdateAggregationBar(DataGrid grid, GridAggregationBar aggregationBar)
        {
            var result = MathOperationsHelper.CalculateStatistics(grid);
            aggregationBar.UpdateStatistics(result);
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

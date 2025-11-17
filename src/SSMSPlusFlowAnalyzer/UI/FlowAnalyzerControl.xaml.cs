using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SSMSPlusCore.Di;

namespace SSMSPlusFlowAnalyzer.UI
{
    /// <summary>
    /// Interaction logic for FlowAnalyzerControl.xaml
    /// </summary>
    public partial class FlowAnalyzerControl : UserControl
    {
        public FlowAnalyzerControl()
        {
            InitializeComponent();

            // Set up data context
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null)
            {
                try
                {
                    var vm = ServiceLocator.GetRequiredService<FlowAnalyzerControlVM>();
                    this.DataContext = vm;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error initializing Flow Analyzer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    /// <summary>
    /// Converter to show/hide elements based on zero count
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets whether to show when count is zero
        /// </summary>
        public bool ShowWhenZero { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ShowWhenZero ? Visibility.Visible : Visibility.Collapsed;

            int count = 0;
            if (value is int intValue)
            {
                count = intValue;
            }

            bool isZero = count == 0;

            if (ShowWhenZero)
            {
                return isZero ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isZero ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

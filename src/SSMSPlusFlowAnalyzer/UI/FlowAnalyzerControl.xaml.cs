using System;
using System.Globalization;
using System.IO;
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
            WriteDebugLog("OnLoaded: Flow Analyzer Control loaded");

            if (this.DataContext == null)
            {
                try
                {
                    WriteDebugLog("OnLoaded: DataContext is null, getting ViewModel from ServiceLocator");
                    var vm = ServiceLocator.GetRequiredService<FlowAnalyzerControlVM>();
                    WriteDebugLog($"OnLoaded: ViewModel retrieved successfully: {vm != null}");

                    this.DataContext = vm;
                    WriteDebugLog("OnLoaded: DataContext set successfully");

                    MessageBox.Show("Flow Analyzer initialized successfully!\nViewModel connected.\nClick Analyze to test.",
                        "Flow Analyzer Init", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"OnLoaded: ERROR - {ex.Message}\n{ex.StackTrace}");
                    MessageBox.Show($"Error initializing Flow Analyzer:\n{ex.Message}\n\nStack:\n{ex.StackTrace}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                WriteDebugLog("OnLoaded: DataContext already set");
            }
        }

        private void WriteDebugLog(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SSMS Plus", "log", "FlowAnalyzer_Debug.txt");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath));

                File.AppendAllText(logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}\n");
            }
            catch
            {
                // Ignore logging errors
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

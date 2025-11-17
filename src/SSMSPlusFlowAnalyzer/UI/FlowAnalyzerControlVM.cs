using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using SSMSPlusCore.Ui;
using SSMSPlusFlowAnalyzer.Entities;
using SSMSPlusFlowAnalyzer.Services;

namespace SSMSPlusFlowAnalyzer.UI
{
    /// <summary>
    /// View model for the Flow Analyzer control
    /// </summary>
    public class FlowAnalyzerControlVM : INotifyPropertyChanged
    {
        private readonly FlowAnalysisService _analysisService;
        private readonly ILogger<FlowAnalyzerControlVM> _logger;

        private SqlFlowAnalysisResult _analysisResult;
        private FlowNodeVM _selectedNode;
        private bool _isAnalyzing;
        private string _statusMessage;
        private string _summaryText;

        public FlowAnalyzerControlVM(
            FlowAnalysisService analysisService,
            ILogger<FlowAnalyzerControlVM> logger)
        {
            _analysisService = analysisService;
            _logger = logger;

            FlowNodes = new ObservableCollection<FlowNodeVM>();
            ExecutionOrder = new ObservableCollection<FlowNodeVM>();
            Errors = new ObservableCollection<string>();
            Warnings = new ObservableCollection<string>();

            AnalyzeCommand = new Command(ExecuteAnalyze);
            RefreshCommand = new Command(ExecuteRefresh);

            StatusMessage = "Ready. Click 'Analyze' to analyze SQL control flow.";
        }

        public ICommand AnalyzeCommand { get; }
        public ICommand RefreshCommand { get; }

        public ObservableCollection<FlowNodeVM> FlowNodes { get; }
        public ObservableCollection<FlowNodeVM> ExecutionOrder { get; }
        public ObservableCollection<string> Errors { get; }
        public ObservableCollection<string> Warnings { get; }

        public SqlFlowAnalysisResult AnalysisResult
        {
            get => _analysisResult;
            set
            {
                _analysisResult = value;
                OnPropertyChanged();
                UpdateDisplay();
            }
        }

        public FlowNodeVM SelectedNode
        {
            get => _selectedNode;
            set
            {
                _selectedNode = value;
                OnPropertyChanged();
            }
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set
            {
                _isAnalyzing = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string SummaryText
        {
            get => _summaryText;
            set
            {
                _summaryText = value;
                OnPropertyChanged();
            }
        }

        private void ExecuteAnalyze()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    IsAnalyzing = true;
                    StatusMessage = "Analyzing SQL control flow...";

                    _logger.LogInformation("Starting SQL flow analysis");

                    var result = _analysisService.AnalyzeActiveDocument();

                    _logger.LogInformation($"Analysis complete. Nodes: {result.AllNodes.Count}, Errors: {result.Errors.Count}");

                    if (result.Errors.Count > 0)
                    {
                        _logger.LogWarning($"Analysis completed with errors: {string.Join(", ", result.Errors)}");
                    }

                    AnalysisResult = result;

                    if (result.Errors.Count > 0)
                    {
                        StatusMessage = $"Analysis completed with {result.Errors.Count} error(s)";
                    }
                    else if (result.AllNodes.Count == 0)
                    {
                        StatusMessage = "No control flow structures found in the SQL";
                        _logger.LogInformation("No control flow structures found");
                    }
                    else
                    {
                        StatusMessage = $"Analysis complete: {result.GetSummary()}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during analysis");
                    StatusMessage = $"Error: {ex.Message}";

                    // Add error to visible errors list
                    Errors.Clear();
                    Errors.Add($"Analysis Error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Errors.Add($"Inner Exception: {ex.InnerException.Message}");
                    }
                }
                finally
                {
                    IsAnalyzing = false;
                }
            });
        }

        private void ExecuteRefresh()
        {
            ExecuteAnalyze();
        }

        private void UpdateDisplay()
        {
            FlowNodes.Clear();
            ExecutionOrder.Clear();
            Errors.Clear();
            Warnings.Clear();

            if (AnalysisResult == null)
            {
                _logger.LogWarning("UpdateDisplay called with null AnalysisResult");
                return;
            }

            _logger.LogInformation($"UpdateDisplay: Processing {AnalysisResult.AllNodes.Count} nodes");

            // Build tree structure
            if (AnalysisResult.RootNode != null)
            {
                _logger.LogInformation($"Building tree from root node with {AnalysisResult.RootNode.Children.Count} children");
                var rootVM = BuildTreeViewModel(AnalysisResult.RootNode);
                if (rootVM != null && rootVM.Children.Count > 0)
                {
                    foreach (var child in rootVM.Children)
                    {
                        FlowNodes.Add(child);
                    }
                    _logger.LogInformation($"Added {FlowNodes.Count} root-level nodes to tree");
                }
                else
                {
                    _logger.LogWarning("Root VM has no children to display");
                }
            }

            // Build execution order list - show ALL nodes in execution order
            if (AnalysisResult.ExecutionOrder != null)
            {
                _logger.LogInformation($"Building execution order from {AnalysisResult.ExecutionOrder.Count} nodes");
                foreach (var node in AnalysisResult.ExecutionOrder)
                {
                    ExecutionOrder.Add(new FlowNodeVM(node));
                }
                _logger.LogInformation($"Added {ExecutionOrder.Count} nodes to execution order");
            }

            // Add errors and warnings
            if (AnalysisResult.Errors != null)
            {
                foreach (var error in AnalysisResult.Errors)
                {
                    Errors.Add(error);
                }
                _logger.LogInformation($"Added {Errors.Count} errors");
            }

            if (AnalysisResult.Warnings != null)
            {
                foreach (var warning in AnalysisResult.Warnings)
                {
                    Warnings.Add(warning);
                }
                _logger.LogInformation($"Added {Warnings.Count} warnings");
            }

            // Update summary
            SummaryText = AnalysisResult.GetSummary();
            _logger.LogInformation($"Summary: {SummaryText}");
        }

        private FlowNodeVM BuildTreeViewModel(FlowNode node)
        {
            if (node == null) return null;

            var vm = new FlowNodeVM(node);

            foreach (var child in node.Children)
            {
                var childVM = BuildTreeViewModel(child);
                if (childVM != null)
                {
                    vm.Children.Add(childVM);
                }
            }

            return vm;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// View model wrapper for FlowNode
    /// </summary>
    public class FlowNodeVM : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;

        public FlowNodeVM(FlowNode node)
        {
            Node = node;
            Children = new ObservableCollection<FlowNodeVM>();
            IsExpanded = true; // Expand by default
        }

        public FlowNode Node { get; }

        public ObservableCollection<FlowNodeVM> Children { get; }

        public string DisplayText => Node.DisplayName;

        public string NodeTypeText => Node.NodeType.ToString();

        public string LocationText => $"Line {Node.LineNumber}, Level {Node.NestingLevel}";

        public string FullInfo =>
            $"{Node.DisplayName}\n" +
            $"Type: {Node.NodeType}\n" +
            $"Line: {Node.LineNumber}\n" +
            $"Nesting Level: {Node.NestingLevel}\n" +
            $"Execution Order: {Node.ExecutionOrder}";

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

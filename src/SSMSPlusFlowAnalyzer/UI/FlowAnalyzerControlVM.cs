using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using SSMSPlusCore.Ui.Commands;
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

            AnalyzeCommand = new DelegateCommand(ExecuteAnalyze);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);

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
            try
            {
                IsAnalyzing = true;
                StatusMessage = "Analyzing SQL control flow...";

                var result = _analysisService.AnalyzeActiveDocument();
                AnalysisResult = result;

                if (result.Errors.Count > 0)
                {
                    StatusMessage = $"Analysis completed with {result.Errors.Count} error(s)";
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
            }
            finally
            {
                IsAnalyzing = false;
            }
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
                return;
            }

            // Build tree structure
            if (AnalysisResult.RootNode != null)
            {
                var rootVM = BuildTreeViewModel(AnalysisResult.RootNode);
                if (rootVM != null && rootVM.Children.Count > 0)
                {
                    foreach (var child in rootVM.Children)
                    {
                        FlowNodes.Add(child);
                    }
                }
            }

            // Build execution order list
            if (AnalysisResult.ExecutionOrder != null)
            {
                foreach (var node in AnalysisResult.ExecutionOrder.Where(n => n.NodeType != FlowNodeType.BeginEnd || n.Parent == null))
                {
                    ExecutionOrder.Add(new FlowNodeVM(node));
                }
            }

            // Add errors and warnings
            foreach (var error in AnalysisResult.Errors)
            {
                Errors.Add(error);
            }

            foreach (var warning in AnalysisResult.Warnings)
            {
                Warnings.Add(warning);
            }

            // Update summary
            SummaryText = AnalysisResult.GetSummary();
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

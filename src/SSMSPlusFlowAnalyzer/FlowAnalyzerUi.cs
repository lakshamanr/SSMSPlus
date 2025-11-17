using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SSMSPlusCore.Integration;
using SSMSPlusFlowAnalyzer.UI;

namespace SSMSPlusFlowAnalyzer
{
    /// <summary>
    /// Handles UI registration for the Flow Analyzer
    /// </summary>
    public class FlowAnalyzerUi
    {
        public const int CommandId = 1301;
        public const int AnalyzeSelectionCommandId = 1302;

        private readonly PackageProvider _packageProvider;
        private readonly FlowAnalyzerControlVM _viewModel;
        private IVsWindowFrame _window;
        private bool _isRegistered = false;

        public FlowAnalyzerUi(PackageProvider packageProvider, FlowAnalyzerControlVM viewModel)
        {
            _packageProvider = packageProvider;
            _viewModel = viewModel;
        }

        public void Register()
        {
            if (_isRegistered)
            {
                throw new Exception("FlowAnalyzerUi is already registered");
            }

            _isRegistered = true;

            // Register main menu command (just opens the window)
            var menuCommandID = new CommandID(MenuHelper.CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            _packageProvider.CommandService.AddCommand(menuItem);

            // Register context menu command (opens window AND triggers analysis)
            var contextCommandID = new CommandID(MenuHelper.CommandSet, AnalyzeSelectionCommandId);
            var contextMenuItem = new MenuCommand(this.ExecuteAnalyzeSelection, contextCommandID);
            _packageProvider.CommandService.AddCommand(contextMenuItem);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ShowToolWindow();
        }

        private void ExecuteAnalyzeSelection(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Show the tool window
            ShowToolWindow();

            // Trigger analysis automatically
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Give the window a moment to fully load
                await System.Threading.Tasks.Task.Delay(100);

                // Trigger the analyze command
                if (_viewModel.AnalyzeCommand.CanExecute(null))
                {
                    _viewModel.AnalyzeCommand.Execute(null);
                }
            });
        }

        private void ShowToolWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var toolWindow = _packageProvider.AsyncPackage.FindToolWindow(typeof(FlowAnalyzerToolWindow), 0, true);
            _window = (IVsWindowFrame)toolWindow.Frame;
            _window.SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_MdiChild);
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(_window.Show());
        }
    }
}

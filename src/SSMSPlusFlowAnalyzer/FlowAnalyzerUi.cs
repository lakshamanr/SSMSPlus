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

        private readonly PackageProvider _packageProvider;
        private IVsWindowFrame _window;
        private bool _isRegistered = false;

        public FlowAnalyzerUi(PackageProvider packageProvider)
        {
            _packageProvider = packageProvider;
        }

        public void Register()
        {
            if (_isRegistered)
            {
                throw new Exception("FlowAnalyzerUi is already registered");
            }

            _isRegistered = true;

            var menuCommandID = new CommandID(MenuHelper.CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);

            _packageProvider.CommandService.AddCommand(menuItem);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var toolWindow = _packageProvider.AsyncPackage.FindToolWindow(typeof(FlowAnalyzerToolWindow), 0, true);
            _window = (IVsWindowFrame)toolWindow.Frame;
            _window.SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_MdiChild);
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(_window.Show());
        }
    }
}

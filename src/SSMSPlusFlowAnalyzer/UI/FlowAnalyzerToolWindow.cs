using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SSMSPlusFlowAnalyzer.UI
{
    /// <summary>
    /// This class implements the tool window for the Flow Analyzer
    /// </summary>
    [Guid("f8e9c7d6-a4b3-4e2f-9d1c-5a8b7c6d9e0f")]
    public class FlowAnalyzerToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the FlowAnalyzerToolWindow class
        /// </summary>
        public FlowAnalyzerToolWindow() : base(null)
        {
            this.Caption = "SQL Flow Analyzer";

            // This is the user control hosted by the tool window
            this.Content = new FlowAnalyzerControl();
        }
    }
}

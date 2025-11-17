using System;

namespace SSMSPlusFlowAnalyzer
{
    /// <summary>
    /// Main plugin class for the Flow Analyzer
    /// </summary>
    public class FlowAnalyzerPlugin
    {
        private bool _isRegistered = false;
        private readonly FlowAnalyzerUi _flowAnalyzerUi;

        public FlowAnalyzerPlugin(FlowAnalyzerUi flowAnalyzerUi)
        {
            _flowAnalyzerUi = flowAnalyzerUi;
        }

        public void Register()
        {
            if (_isRegistered)
            {
                throw new Exception("FlowAnalyzerPlugin is already registered");
            }

            _isRegistered = true;
            _flowAnalyzerUi.Register();
        }
    }
}

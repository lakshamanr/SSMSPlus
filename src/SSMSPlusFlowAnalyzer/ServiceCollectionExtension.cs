using Microsoft.Extensions.DependencyInjection;
using SSMSPlusFlowAnalyzer.Services;
using SSMSPlusFlowAnalyzer.UI;

namespace SSMSPlusFlowAnalyzer
{
    /// <summary>
    /// Extension methods for registering Flow Analyzer services
    /// </summary>
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddSSMSPlusFlowAnalyzerServices(this IServiceCollection services)
        {
            // Register services
            services.AddSingleton<SqlTextProvider>();
            services.AddSingleton<FlowAnalysisService>();
            services.AddSingleton<DiagnosticService>();

            // Register UI components
            services.AddSingleton<FlowAnalyzerUi>();
            services.AddSingleton<FlowAnalyzerControlVM>();

            // Register plugin
            services.AddSingleton<FlowAnalyzerPlugin>();

            return services;
        }
    }
}

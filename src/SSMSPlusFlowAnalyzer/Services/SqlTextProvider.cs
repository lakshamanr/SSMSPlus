using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using SSMSPlusCore.Integration;

namespace SSMSPlusFlowAnalyzer.Services
{
    /// <summary>
    /// Provides SQL text from the active SSMS editor
    /// </summary>
    public class SqlTextProvider
    {
        private readonly PackageProvider _packageProvider;
        private readonly ILogger<SqlTextProvider> _logger;

        public SqlTextProvider(PackageProvider packageProvider, ILogger<SqlTextProvider> logger)
        {
            _packageProvider = packageProvider;
            _logger = logger;
        }

        /// <summary>
        /// Gets the selected SQL text, or entire document if nothing is selected
        /// </summary>
        public string GetSqlText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _logger.LogDebug("Attempting to get SQL text from active document");

                var dte = _packageProvider.Dte2;
                if (dte?.ActiveDocument == null)
                {
                    _logger.LogWarning("No active document found (DTE2.ActiveDocument is null)");
                    return null;
                }

                _logger.LogDebug($"Active document: {dte.ActiveDocument.Name}");

                var textDocument = dte.ActiveDocument.Object("TextDocument") as TextDocument;
                if (textDocument == null)
                {
                    _logger.LogWarning("Could not get TextDocument object from active document");
                    return null;
                }

                // Try to get selected text first (matching pattern from QueryTracker.GetQueryText)
                string queryText = textDocument.Selection?.Text;

                if (!string.IsNullOrEmpty(queryText))
                {
                    _logger.LogInformation($"Retrieved selected text: {queryText.Length} characters");
                }
                else
                {
                    _logger.LogDebug("No text selected, getting entire document");

                    // If no selection, get entire document
                    var startPoint = textDocument.StartPoint.CreateEditPoint();
                    queryText = startPoint.GetText(textDocument.EndPoint);

                    _logger.LogInformation($"Retrieved entire document: {queryText?.Length ?? 0} characters");
                }

                return queryText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception in GetSqlText, trying fallback method");

                // Fallback: try using text buffer
                return GetSqlTextFromTextBuffer();
            }
        }

        private string GetSqlTextFromTextBuffer()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _logger.LogDebug("Trying fallback method: GetSqlTextFromTextBuffer");

                // Get the text manager
                var textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
                if (textManager == null)
                {
                    _logger.LogWarning("Could not get SVsTextManager service");
                    return null;
                }

                // Get the active view
                IVsTextView textView;
                textManager.GetActiveView(1, null, out textView);
                if (textView == null)
                {
                    _logger.LogWarning("Could not get active text view");
                    return null;
                }

                // Get the text buffer
                IVsTextLines textLines;
                textView.GetBuffer(out textLines);
                if (textLines == null)
                {
                    _logger.LogWarning("Could not get text buffer");
                    return null;
                }

                // Check for selection
                int startLine, startCol, endLine, endCol;
                textView.GetSelection(out startLine, out startCol, out endLine, out endCol);

                string text;
                if (startLine != endLine || startCol != endCol)
                {
                    _logger.LogDebug($"Getting selected text from buffer (L{startLine}:C{startCol} to L{endLine}:C{endCol})");

                    // Get selected text
                    textLines.GetLineText(startLine, startCol, endLine, endCol, out text);
                }
                else
                {
                    _logger.LogDebug("Getting entire buffer text");

                    // Get entire buffer
                    int lastLine, lastIndex;
                    textLines.GetLastLineIndex(out lastLine, out lastIndex);
                    textLines.GetLineText(0, 0, lastLine, lastIndex, out text);
                }

                _logger.LogInformation($"Fallback method retrieved: {text?.Length ?? 0} characters");

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetSqlTextFromTextBuffer fallback method");
                return null;
            }
        }

        /// <summary>
        /// Gets information about the active document
        /// </summary>
        public string GetActiveDocumentInfo()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = _packageProvider.Dte2;
                if (dte?.ActiveDocument == null)
                {
                    return "No active document";
                }

                return $"Document: {dte.ActiveDocument.Name}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}

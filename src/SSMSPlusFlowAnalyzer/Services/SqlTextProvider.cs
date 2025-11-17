using System;
using EnvDTE;
using EnvDTE80;
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

        public SqlTextProvider(PackageProvider packageProvider)
        {
            _packageProvider = packageProvider;
        }

        /// <summary>
        /// Gets the selected SQL text, or entire document if nothing is selected
        /// </summary>
        public string GetSqlText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = _packageProvider.Dte2;
                if (dte?.ActiveDocument == null)
                {
                    return null;
                }

                var textDocument = dte.ActiveDocument.Object("TextDocument") as TextDocument;
                if (textDocument == null)
                {
                    return null;
                }

                // Get selection
                var selection = textDocument.Selection;
                if (selection != null && !selection.IsEmpty)
                {
                    return selection.Text;
                }

                // Get entire document
                var startPoint = textDocument.StartPoint.CreateEditPoint();
                var endPoint = textDocument.EndPoint;
                return startPoint.GetText(endPoint);
            }
            catch (Exception)
            {
                // Fallback: try using text buffer
                return GetSqlTextFromTextBuffer();
            }
        }

        private string GetSqlTextFromTextBuffer()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Get the text manager
                var textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager;
                if (textManager == null)
                {
                    return null;
                }

                // Get the active view
                IVsTextView textView;
                textManager.GetActiveView(1, null, out textView);
                if (textView == null)
                {
                    return null;
                }

                // Get the text buffer
                IVsTextLines textLines;
                textView.GetBuffer(out textLines);
                if (textLines == null)
                {
                    return null;
                }

                // Check for selection
                int startLine, startCol, endLine, endCol;
                textView.GetSelection(out startLine, out startCol, out endLine, out endCol);

                string text;
                if (startLine != endLine || startCol != endCol)
                {
                    // Get selected text
                    textLines.GetLineText(startLine, startCol, endLine, endCol, out text);
                }
                else
                {
                    // Get entire buffer
                    int lastLine, lastIndex;
                    textLines.GetLastLineIndex(out lastLine, out lastIndex);
                    textLines.GetLineText(0, 0, lastLine, lastIndex, out text);
                }

                return text;
            }
            catch (Exception)
            {
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

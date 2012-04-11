using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HighlightCommentKinds
{
    [Export(typeof(EditorFormatDefinition))]
    [Name(Constants.HighlightCommentKindsTagName)]
    [UserVisible(true)]
    [Order(After = Priority.Default)]
    internal sealed class HighlightCommentKindsFormat : MarkerFormatDefinition
    {
        /// <summary>
        /// Defines the visual format for the "HighlightCommentKinds" classification type
        /// </summary>
        internal HighlightCommentKindsFormat()
        {
            this.DisplayName = "Highlight Comment Kinds"; //human readable version of the name
            this.ForegroundColor = Colors.Red;
        }
    }
}

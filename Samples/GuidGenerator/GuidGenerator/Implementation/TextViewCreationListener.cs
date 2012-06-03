using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace VsSamples.GuidGenerator.Implementation
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class TextViewCreationListener : IWpfTextViewCreationListener
    {
        void IWpfTextViewCreationListener.TextViewCreated(IWpfTextView textView)
        {
            var generator = new TextViewGenerator(textView);
            textView.Closed += (sender, e) => generator.Close();
        }
    }
}

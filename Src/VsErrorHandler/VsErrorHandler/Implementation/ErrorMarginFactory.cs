using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace VsErrorHandler.Implementation
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(ErrorMargin.MarginName)]
    [MarginContainer(PredefinedMarginNames.Top)]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class ErrorMarginFactory : IWpfTextViewMarginProvider
    {
        private readonly IErrorData _errorData;

        [ImportingConstructor]
        internal ErrorMarginFactory(IErrorData errorData)
        {
            _errorData = errorData;
        }

        IWpfTextViewMargin IWpfTextViewMarginProvider.CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            return new ErrorMargin(textViewHost.TextView, _errorData);
        }
    }
}

using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;

namespace VsErrorHandler.Implementation
{
    internal sealed class ErrorMargin : IWpfTextViewMargin
    {
        internal const string MarginName = "ErrorMargin";

        private readonly ITextView _textView;
        private readonly ErrorMarginControl _errorMarginControl;
        private readonly IErrorData _errorData;

        internal ErrorMargin(ITextView textView, IErrorData errorData)
        {
            _textView = textView;
            _errorData = errorData;
            _errorMarginControl = new ErrorMarginControl();
            _errorMarginControl.Visibility = Visibility.Collapsed;
            _errorMarginControl.IgnoreError += OnIgnoreError;
            _errorMarginControl.IgnoreAllErrors += OnIgnoreAllErrors;

            _errorData.ErrorThrown += OnErrorThrown;
            _errorData.ErrorIgnored += OnErrorIgnored;
            _textView.Closed += OnTextViewClosed;
        }

        private void OnErrorThrown(object sender, ExceptionEventArgs e)
        {
            _errorMarginControl.Exception = e.Exception;
            _errorMarginControl.Visibility = Visibility.Visible;
        }

        private void OnErrorIgnored(object sender, EventArgs e)
        {
            _errorMarginControl.Exception = null;
            _errorMarginControl.ErrorTextVisibility = Visibility.Collapsed;
            _errorMarginControl.Visibility = Visibility.Collapsed;
        }

        private void OnIgnoreError(object sender, EventArgs e)
        {
            _errorData.IgnoreLastError();
        }

        private void OnIgnoreAllErrors(object sender, EventArgs e)
        {
            _errorData.IgnoreAll = true;
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            _errorData.ErrorThrown -= OnErrorThrown;
            _errorData.ErrorIgnored -= OnErrorIgnored;
            _errorMarginControl.IgnoreError -= OnIgnoreError;
            _errorMarginControl.IgnoreAllErrors -= OnIgnoreAllErrors;
        }

        #region IWpfTextViewMargin

        FrameworkElement IWpfTextViewMargin.VisualElement
        {
            get { return _errorMarginControl; }
        }

        bool ITextViewMargin.Enabled
        {
            get { return true; }
        }

        ITextViewMargin ITextViewMargin.GetTextViewMargin(string marginName)
        {
            return marginName == MarginName ? this : null;
        }

        double ITextViewMargin.MarginSize
        {
            get { return 25; }
        }

        void IDisposable.Dispose()
        {
            
        }

        #endregion
    }
}
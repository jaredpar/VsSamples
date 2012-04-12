using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;
using System.Diagnostics;

namespace TestErrorMargin
{
    /// <summary>
    /// A class detailing the margin's visual definition including both size and content.
    /// </summary>
    internal sealed class TestErrorMargin : IWpfTextViewMargin
    {
        internal const string MarginName = "TestErrorMargin";
        private readonly ITextView _textView;
        private readonly TestErrorMarginControl _testMarginControl;
        private bool _inThrowError;

        internal TestErrorMargin(ITextView textView)
        {
            _textView = textView;
            _testMarginControl = new TestErrorMarginControl();
            _testMarginControl.ThrowError += OnThrowError;
            _textView.Caret.PositionChanged += OnCaretPositionChanged;
        }

        private void OnThrowError(object sender, EventArgs e)
        {
            try
            {
                _inThrowError = true;
                _textView.Caret.MoveToNextCaretPosition();
            }
            finally
            {
                _inThrowError = false;
            }
        }

        [DebuggerNonUserCode]
        private void OnCaretPositionChanged(object sender, EventArgs e)
        {
            if (_inThrowError)
            {
                throw new Exception("Test Exception");
            }
        }

        #region IWpfTextViewMargin

        FrameworkElement IWpfTextViewMargin.VisualElement
        {
            get { return _testMarginControl; }
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

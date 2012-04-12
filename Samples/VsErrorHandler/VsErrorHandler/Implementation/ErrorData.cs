using System;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;

namespace VsErrorHandler.Implementation
{
    [Export(typeof(IErrorData))]
    [Export(typeof(IExtensionErrorHandler))]
    internal sealed class ErrorData : IErrorData, IExtensionErrorHandler
    {
        private bool _ignoreAll;
        private event EventHandler<ExceptionEventArgs> _errorThrownEvent;
        private event EventHandler _errorIgnoredEvent;

        internal ErrorData()
        {

        }

        private void RaiseIgnoreLastError()
        {
            if (_errorIgnoredEvent != null)
            {
                _errorIgnoredEvent(this, EventArgs.Empty);
            }
        }

        #region IExtensionErrorHandler

        void IExtensionErrorHandler.HandleError(object sender, Exception exception)
        {
            if (_ignoreAll)
            {
                return;
            }

            if (_errorThrownEvent != null)
            {
                var args = new ExceptionEventArgs(exception);
                _errorThrownEvent(this, args);
            }
        }

        #endregion

        #region IErrorData

        bool IErrorData.IgnoreAll
        {
            get { return _ignoreAll; }
            set 
            { 
                _ignoreAll = value;
                RaiseIgnoreLastError();
            }
        }

        void IErrorData.IgnoreLastError()
        {
            RaiseIgnoreLastError();
        }

        event EventHandler<ExceptionEventArgs> IErrorData.ErrorThrown
        {
            add { _errorThrownEvent += value; }
            remove { _errorThrownEvent -= value; }
        }

        event EventHandler IErrorData.ErrorIgnored
        {
            add { _errorIgnoredEvent += value; }
            remove { _errorIgnoredEvent -= value; }
        }

        #endregion
    }
}

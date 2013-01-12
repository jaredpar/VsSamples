using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace VsErrorHandler
{
    internal sealed class ExceptionEventArgs : EventArgs
    {
        private readonly Exception _exception;

        internal Exception Exception
        {
            get { return _exception; }
        }

        internal ExceptionEventArgs(Exception exception)
        {
            _exception = exception;
        }
    }

    internal interface IErrorData
    {
        bool IgnoreAll { get; set; }
        void IgnoreLastError();
        event EventHandler<ExceptionEventArgs> ErrorThrown;
        event EventHandler ErrorIgnored;
    }
}

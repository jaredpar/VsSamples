using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectionBufferDemo.Implementation.EditorFactory
{
    [Export(typeof(IEditorFactory))]
    internal sealed class EditorFactory : IEditorFactory
    {
        private readonly VsEditorFactory _vsEditorFactory;

        internal EditorFactory()
        {
            _vsEditorFactory = new VsEditorFactory();
        }

        #region IEditorFactory

        IVsEditorFactory IEditorFactory.VsEditorFactory
        {
            get { return _vsEditorFactory; }
        }

        #endregion
    }
}

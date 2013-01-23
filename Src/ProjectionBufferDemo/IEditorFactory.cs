using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TextManager.Interop;

namespace ProjectionBufferDemo
{
    internal interface IEditorFactory
    {
        IVsEditorFactory VsEditorFactory { get; }

        ITextBufferFactoryService TextBufferFactoryService { get; }

        /// <summary>
        /// This will create an initialized IVsTextBuffer wrapper for the given ITextBuffer instance.  It will create
        /// the full shim which is initialized to use the specified ITextBuffer
        /// </summary>
        IVsTextBuffer CreateVsTextBuffer(ITextBuffer textBuffer, string name);

        /// <summary>
        /// This will create an initialied IVsTextView wrapper for the given ITextBuffer instance.  It will create
        /// the full shim which is initialized to the specified ITextBuffer
        /// </summary>
        IVsTextView CreateVsTextView(IVsTextBuffer vsTextBuffer, params string[] textViewRoles);

        bool OpenInNewWindow(
            ITextBuffer textBuffer,
            string name,
            Guid? editorTypeId = null,
            Guid? logicalViewId = null,
            Guid? languageServiceId = null);
    }
}

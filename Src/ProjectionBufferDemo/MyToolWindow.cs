using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio;

namespace ProjectionBufferDemo
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("9deee938-cb7c-4f5a-90f4-6c70d3ac07bf")]
    public class MyToolWindow : ToolWindowPane, IOleCommandTarget
    {
        // HACK
        internal static ITextEditorFactoryService TextEditorFactoryService;
        internal static IVsEditorAdaptersFactoryService VsEditorAdaptersFactoryService;
        internal static IOleServiceProvider OleServiceProvider;
        internal static IEditorFactory EditorFactory;

        private readonly ITextBuffer _textBuffer;
        private readonly IVsTextBuffer _vsTextBuffer;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public MyToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.ToolWindowTitle;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            _vsTextBuffer = VsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(OleServiceProvider);

            // Set the content type 
            var contentTypeKey = new Guid(0x1beb4195, 0x98f4, 0x4589, 0x80, 0xe0, 0x48, 12, 0xe3, 0x2f, 240, 0x59);
            var vsUserData = (IVsUserData)_vsTextBuffer;
            vsUserData.SetData(ref contentTypeKey, "text");

            _vsTextBuffer.InitializeContent("", 0);
            _textBuffer = VsEditorAdaptersFactoryService.GetDataBuffer(_vsTextBuffer);

            var vsTextView = EditorFactory.CreateVsTextView(
                _vsTextBuffer, 
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.PrimaryDocument);

            var wpfTextViewHost = VsEditorAdaptersFactoryService.GetWpfTextViewHost(vsTextView);
            base.Content = wpfTextViewHost.HostControl;

            var oleCommandTarget = _vsTextBuffer as IOleCommandTarget;
            if (oleCommandTarget != null)
            {
                ToolBarCommandTarget = oleCommandTarget;
            }
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var oleCommandTarget = _vsTextBuffer as IOleCommandTarget;
            if (oleCommandTarget != null)
            {
                return oleCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }

        /// <summary>
        /// When our tool window is active it will be the 'focus command target' of the shell's command route, as such we need to set the state
        /// of any commands we want here and forward the rest to the editor (since most all typing is translated into a command for the editor to
        /// deal with).
        /// </summary>
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            var oleCommandTarget = _vsTextBuffer as IOleCommandTarget;
            if (oleCommandTarget != null)
            {
                return oleCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            return (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;
        }
    }
}

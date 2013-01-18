using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace ProjectionBufferDemo.Implementation
{
    [Guid("a28cb5f4-e84d-4be3-900c-895827b8332c")]
    internal sealed class VsEditorFactory : IVsEditorFactory
    {
        // TODO: Remove.  Layering violation
        internal Package Package { get; set; }

        private IServiceProvider _serviceProvider;

        private int MapLogicalView(ref Guid logicalView, out string physicalView)
        {
            physicalView = null;
            return logicalView == VSConstants.LOGVIEWID.Primary_guid
                ? VSConstants.S_OK
                : VSConstants.E_NOTIMPL;
        }

        private int CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption, out Guid pguidCmdUI, out int pgrfCDW)
        {
            int retval = VSConstants.E_FAIL;

            // Initialize these to empty to start with 
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pbstrEditorCaption = "";
            pguidCmdUI = Guid.Empty;
            pgrfCDW = 0;

            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
            {
                throw new ArgumentException("Only Open or Silent is valid");
            }
            if (punkDocDataExisting != IntPtr.Zero)
            {
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }

            // Instantiate a text buffer of type VsTextBuffer. 
            // Note: we only need an IUnknown (object) interface for 
            // this invocation. 
            Guid clsidTextBuffer = typeof(VsTextBufferClass).GUID;
            Guid iidTextBuffer = VSConstants.IID_IUnknown;
            object pTextBuffer = pTextBuffer = Package.CreateInstance(
                                                        ref clsidTextBuffer,
                                                        ref iidTextBuffer,
                                                        typeof(object));

            if (pTextBuffer != null)
            {
                // "Site" the text buffer with the service provider we were 
                // provided. 
                IObjectWithSite textBufferSite = pTextBuffer as IObjectWithSite;
                if (textBufferSite != null)
                {
                    textBufferSite.SetSite(_serviceProvider);
                }

                // Instantiate a code window of type IVsCodeWindow. 
                Guid clsidCodeWindow = typeof(VsCodeWindowClass).GUID;
                Guid iidCodeWindow = typeof(IVsCodeWindow).GUID;
                IVsCodeWindow pCodeWindow =
                    (IVsCodeWindow)Package.CreateInstance(
                                                    ref clsidCodeWindow,
                                                    ref iidCodeWindow,
                                                    typeof(IVsCodeWindow));
                if (pCodeWindow != null)
                {
                    // Give the text buffer to the code window. 
                    // We are giving up ownership of the text buffer! 
                    pCodeWindow.SetBuffer((IVsTextLines)pTextBuffer);

                    // Now tell the caller about all this new stuff 
                    // that has been created. 
                    ppunkDocView = Marshal.GetIUnknownForObject(pCodeWindow);
                    ppunkDocData = Marshal.GetIUnknownForObject(pTextBuffer);

                    // Specify the command UI to use so keypresses are 
                    // automatically dealt with. 
                    pguidCmdUI = VSConstants.GUID_TextEditorFactory;

                    // This caption is appended to the filename and 
                    // lets us know our invocation of the core editor 
                    // is up and running. 
                    pbstrEditorCaption = " [MyPackage]";

                    retval = VSConstants.S_OK;
                }
            }
            return retval;
        }

        #region IVsEditorFactory

        int IVsEditorFactory.Close()
        {
            return VSConstants.S_OK;
        }

        int IVsEditorFactory.CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption, out Guid pguidCmdUI, out int pgrfCDW)
        {
            return CreateEditorInstance(grfCreateDoc, pszMkDocument, pszPhysicalView, pvHier, itemid, punkDocDataExisting, out ppunkDocView, out ppunkDocData, out pbstrEditorCaption, out pguidCmdUI, out pgrfCDW);
        }

        int IVsEditorFactory.MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            return MapLogicalView(ref rguidLogicalView, out pbstrPhysicalView);
        }

        int IVsEditorFactory.SetSite(IServiceProvider psp)
        {
            _serviceProvider = psp;
            return VSConstants.S_OK;
        }

        #endregion
    }
}

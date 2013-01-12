using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaredPar.ProjectionBufferDemo.Implementation.EditorFactory
{
    internal sealed class VsEditorFactory : IVsEditorFactory
    {
        #region IVsEditorFactory

        int IVsEditorFactory.Close()
        {
            return VSConstants.S_OK;
        }

        int IVsEditorFactory.CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption, out Guid pguidCmdUI, out int pgrfCDW)
        {
            throw new NotImplementedException();
        }

        int IVsEditorFactory.MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            throw new NotImplementedException();
        }

        int IVsEditorFactory.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

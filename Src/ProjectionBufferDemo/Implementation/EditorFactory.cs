using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IServiceProvider = System.IServiceProvider;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using System.Reflection;

namespace ProjectionBufferDemo.Implementation
{
    [Export(typeof(IEditorFactory))]
    internal sealed class EditorFactory : IEditorFactory
    {
        private readonly VsEditorFactory _vsEditorFactory;
        private readonly SVsServiceProvider _vsServiceProvider;
        private readonly IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;

        [ImportingConstructor]
        internal EditorFactory(
            SVsServiceProvider vsServiceProvider,
            IVsEditorAdaptersFactoryService vsEditorAdaptersAdapterFactory,
            ITextDocumentFactoryService textDocumentFactoryService)
        {
            _vsEditorFactory = new VsEditorFactory();
            _vsServiceProvider = vsServiceProvider;
            _vsEditorAdaptersFactoryService = vsEditorAdaptersAdapterFactory;
            _textDocumentFactoryService = textDocumentFactoryService;
        }

        private static string GetMoniker(string baseName)
        {
            // TODO: should be unique
            return String.Format("{0}.testext", baseName);
        }

        private bool OpenInNewWindow(string name, ITextBuffer textBuffer)
        {
            var moniker = GetMoniker(name);
            IVsUIHierarchy vsUiHierarchy;
            uint itemId;
            if (!TryCreateHierarchy(moniker, out vsUiHierarchy, out itemId))
            {
                return false;
            }

            var uiShell = _vsServiceProvider.GetService<SVsUIShell, IVsUIShell>();
            var uiShellOpenDocument = _vsServiceProvider.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();

            IntPtr punkTextBuffer = IntPtr.Zero;
            uint documentCookie = VSConstants.VSCOOKIE_NIL;

            try
            {
                // Wrap the result's ITextBuffer so that it can be used with the old VS APIs
                var oleServiceProvider = _vsServiceProvider.GetService<IOleServiceProvider, IOleServiceProvider>();
                var vsTextBuffer = _vsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(oleServiceProvider, textBuffer.ContentType);
                var textDocument = _textDocumentFactoryService.CreateTextDocument(textBuffer, name);

                // TODO: Should I set a language service?   If we do it must come before creating the code
                // window below .  Maybe make it customizable

                // HACK OF EVIL HACKS: Since there is currently no way to create a text buffer shim
                // for an ITextBuffer that we have already created, we have to create an empty shim,
                // and initialize its underlying ITextBuffer ourselves via reflection
                var vsTextBufferType = vsTextBuffer.GetType();
                vsTextBufferType.GetField("_documentTextBuffer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vsTextBuffer, textBuffer);
                vsTextBufferType.GetField("_surfaceTextBuffer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vsTextBuffer, textBuffer);
                vsTextBufferType.GetField("_textDocument", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vsTextBuffer, textDocument);
                vsTextBufferType.GetMethod("InitializeDocumentTextBuffer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(textBuffer, null);
                
                /*
                var hr = vsTextBuffer.SetStateFlags((uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
                if (ErrorHandler.Failed(hr))
                {
                    return false;
                }
                */

                // TODO: Set the text view roles now.  Need to do it on the IVsTextBuffer likely

                // Register and obtain a read lock on the document in the running document
                // table so that it stays alive even though it doesn't have an associated
                // editor.
                documentCookie = RegisterAndReadLockDocument(moniker, textBuffer);

                // Open the text buffer in a new window.  NOTE: The reason we need a hierarchy and an
                // itemId is that the IVsUIShellOpenDocument will search for the project that can
                // open the document unless we tell it the context (miscellaneous files).
                punkTextBuffer = Marshal.GetIUnknownForObject(textBuffer);
                IVsWindowFrame windowFrame;

                // TODO: Let the editor guid be customizable
                var editorType = VSConstants.GUID_TextEditorFactory;
                var guidLogicalView = VSConstants.LOGVIEWID.TextView_guid;
                ErrorHandler.ThrowOnFailure(uiShellOpenDocument.OpenSpecificEditor(
                    grfOpenSpecific: 0,
                    pszMkDocument: moniker,
                    rguidEditorType: editorTypeGuid,
                    pszPhysicalView: "Code",
                    rguidLogicalView: guidLogicalView,
                    pszOwnerCaption: document.Name,
                    pHier: hierarchy,
                    itemid: itemId,
                    punkDocDataExisting: punkTextBuffer,
                    pSPHierContext: oleServiceProvider,
                    ppWindowFrame: out windowFrame));

                GetRunningDocumentTable().ModifyDocumentFlags(documentCookie,
                    (uint)_VSRDTFLAGS.RDT_DontAddToMRU | (uint)_VSRDTFLAGS.RDT_CantSave, fSet: 1);

                if (windowFrame != null)
                {
                    ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, true));

                    this.cookieResultMap.Add(documentCookie, result);
                    this.documentWindowMap.Add(result.DocumentId, new GeneratedDocumentWindow(result, windowFrame));
                }

                return windowFrame;
            }
            finally
            {
                // Remove the read lock on the document in the running document table.  If the
                // editor is open, it will have an EditLock on the document and keep it alive
                // until the editor is closed.
                if (documentCookie != VSConstants.VSCOOKIE_NIL)
                {
                    UnlockDocument(documentCookie);
                }

                if (punkTextBuffer != IntPtr.Zero)
                {
                    Marshal.Release(punkTextBuffer);
                }
            }
        }

        /// <summary>
        /// This will add a file to the misc folders without actually opening the document in 
        /// Visual Studio
        /// </summary>
        private bool TryCreateHierarchy(string moniker, out IVsUIHierarchy vsUiHierarchy, out uint itemId)
        {
            vsUiHierarchy = null;
            itemId = VSConstants.VSITEMID_NIL;

            var vsExternalFilesManager = _vsServiceProvider.GetService<SVsExternalFilesManager, IVsExternalFilesManager>();

            int defaultPosition;
            IVsWindowFrame dummyWindowFrame;
            uint flags = (uint)_VSRDTFLAGS.RDT_NonCreatable | (uint)_VSRDTFLAGS.RDT_PlaceHolderDoc;
            var hr = vsExternalFilesManager.AddDocument(
                dwCDW: flags,
                pszMkDocument: moniker,
                punkDocView: IntPtr.Zero,
                punkDocData: IntPtr.Zero,
                rguidEditorType: Guid.Empty,
                pszPhysicalView: null,
                rguidCmdUI: Guid.Empty,
                pszOwnerCaption: moniker,
                pszEditorCaption: null,
                pfDefaultPosition: out defaultPosition,
                ppWindowFrame: out dummyWindowFrame);
            if (ErrorHandler.Failed(hr))
            {
                return false;
            }

            ErrorHandler.ThrowOnFailure(hr);

            // Get the hierarchy for the document we added to the miscellaneous files project
            IVsProject vsProject;
            hr = vsExternalFilesManager.GetExternalFilesProject(out vsProject);
            if (ErrorHandler.Failed(hr))
            {
                return false;
            }

            int found;
            VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];
            hr = vsProject.IsDocumentInProject(moniker, out found, priority, out itemId);
            if (ErrorHandler.Failed(hr) ||
                0 == found ||
                VSConstants.VSITEMID_NIL == itemId)
            {
                return false;
            }

            vsUiHierarchy = (IVsUIHierarchy)vsProject;
            return true;
        }

        /*
         *        private IVsWindowFrame CreateTrackedWindowFrame(MetadataAsSourceResult result)
        {
        }

        private void CollapseToDefinitions(ITextBuffer textBuffer)
        {
            var vsTextBuffer = editorAdaptersFactory.GetBufferAdapter(textBuffer);

            if (vsTextBuffer == null)
            {
                return;
            }

            var textManager = GetService<SVsTextManager, IVsTextManager>();
            IVsEnumTextViews enumTextViews;

            textManager.EnumViews(vsTextBuffer, out enumTextViews);

            if (enumTextViews == null)
            {
                return;
            }

            var views = new IVsTextView[1];
            uint fetched = 0;

            if (ErrorHandler.Succeeded(enumTextViews.Next(1, views, ref fetched)) && fetched == 1)
            {
                var view = editorAdaptersFactory.GetWpfTextView(views[0]);
                var componentModel = GetService<SComponentModel, IComponentModel>();
                var outliningManagerService = componentModel.GetService<IOutliningManagerService>();
                var outliningManager = outliningManagerService.GetOutliningManager(view);

                outliningManager.CollapseAll(new SnapshotSpan(textBuffer.CurrentSnapshot,
                                                              start: 0,
                                                              length: textBuffer.CurrentSnapshot.Length),
                                             c => c.Tag.IsImplementation);
            }
        }

        private void CreatePlaceHolderDocument(string moniker, out IVsUIHierarchy hierarchy, out uint itemId)
        {
            // Add the document to the miscellaneous files project without opening a window
            var miscDocs = GetService<SVsExternalFilesManager, IVsExternalFilesManager>();

            hierarchy = null;
            itemId = VSConstants.VSITEMID_NIL;
            int defaultPosition;
            IVsWindowFrame dummyWindowFrame;
            uint flags = (uint)_VSRDTFLAGS.RDT_NonCreatable | (uint)_VSRDTFLAGS.RDT_PlaceHolderDoc;

            var hresult = miscDocs.AddDocument(
                dwCDW: flags,
                pszMkDocument: moniker,
                punkDocView: IntPtr.Zero,
                punkDocData: IntPtr.Zero,
                rguidEditorType: Guid.Empty,
                pszPhysicalView: null,
                rguidCmdUI: Guid.Empty,
                pszOwnerCaption: moniker,
                pszEditorCaption: null,
                pfDefaultPosition: out defaultPosition,
                ppWindowFrame: out dummyWindowFrame);

            ErrorHandler.ThrowOnFailure(hresult);

            // Get the hierarchy for the document we added to the miscellaneous files project
            IVsProject project;
            int found;
            VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];

            miscDocs.GetExternalFilesProject(out project);
            project.IsDocumentInProject(moniker, out found, priority, out itemId);

            if (found == 0)
            {
                throw new COMException("Failed to create document in miscellaneous files project".NeedsLocalization(), VSConstants.E_FAIL);
            }

            hierarchy = (IVsUIHierarchy)project;
        }

        private uint RegisterAndReadLockDocument(string moniker, IVsTextBuffer textBuffer)
        {
            var runningDocumentTable = GetRunningDocumentTable();

            uint documentCookie = VSConstants.VSCOOKIE_NIL;
            IntPtr punkDocData = IntPtr.Zero;
            try
            {
                punkDocData = Marshal.GetIUnknownForObject(textBuffer);
                runningDocumentTable.RegisterAndLockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, moniker,
                    null, VSConstants.VSITEMID_NIL, punkDocData, out documentCookie);
            }
            finally
            {
                if (punkDocData != IntPtr.Zero)
                {
                    Marshal.Release(punkDocData);
                }
            }

            return documentCookie;
        }

        private void UnlockDocument(uint documentCookie)
        {
            GetRunningDocumentTable().UnlockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, documentCookie);
        }
        */
 

        #region IEditorFactory

        IVsEditorFactory IEditorFactory.VsEditorFactory
        {
            get { return _vsEditorFactory; }
        }

        bool IEditorFactory.OpenInNewWindow(string name, ITextBuffer textBuffer)
        {
            return OpenInNewWindow(name, textBuffer);
        }

        #endregion
    }
}

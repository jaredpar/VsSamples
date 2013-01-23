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
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using EditorUtils;
using Microsoft.VisualStudio.Text.Editor;

namespace ProjectionBufferDemo.Implementation
{
    [Export(typeof(IEditorFactory))]
    internal sealed class EditorFactory : IEditorFactory
    {
        private readonly VsEditorFactory _vsEditorFactory;
        private readonly SVsServiceProvider _vsServiceProvider;
        private readonly IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IOleServiceProvider _oleServiceProvider;
        private readonly IProtectedOperations _protectedOperations;

        [ImportingConstructor]
        internal EditorFactory(
            SVsServiceProvider vsServiceProvider,
            IVsEditorAdaptersFactoryService vsEditorAdaptersAdapterFactory,
            ITextBufferFactoryService textBufferFactoryService,
            ITextDocumentFactoryService textDocumentFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            [EditorUtilsImport] IProtectedOperations protectedOperations)
        {
            _vsEditorFactory = new VsEditorFactory();
            _vsServiceProvider = vsServiceProvider;
            _vsEditorAdaptersFactoryService = vsEditorAdaptersAdapterFactory;
            _textBufferFactoryService = textBufferFactoryService;
            _textDocumentFactoryService = textDocumentFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _oleServiceProvider = _vsServiceProvider.GetService<IOleServiceProvider, IOleServiceProvider>();
            _protectedOperations = protectedOperations;
        }

        /// <summary>
        /// Generate a unique string based on the specified name.  This name isn't displayed to the user
        /// just make it unique
        /// </summary>
        private static string GetMoniker(string baseName)
        {
            var guid = Guid.NewGuid().ToString("D");
            return String.Format("{0}.{1}.hidden", baseName, guid);
        }

        private bool OpenInNewWindow(
            ITextBuffer textBuffer,
            string name,
            Guid? editorTypeGuid = null,
            Guid? languageServiceGuid = null,
            Guid? logicalViewGuid = null)
        {
            var moniker = GetMoniker(name);
            var uiShell = _vsServiceProvider.GetService<SVsUIShell, IVsUIShell>();
            var uiShellOpenDocument = _vsServiceProvider.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();

            IntPtr punkTextBuffer = IntPtr.Zero;
            uint documentCookie = VSConstants.VSCOOKIE_NIL;
            try
            {
                IVsUIHierarchy vsUiHierarchy;
                uint itemId;
                CreateHierarchy(moniker, out vsUiHierarchy, out itemId);

                // Wrap the result's ITextBuffer so that it can be used with the old VS APIs
                var vsTextBuffer = CreateVsTextBuffer(textBuffer, name, languageServiceGuid);

                // Register and obtain a read lock on the document in the running document
                // table so that it stays alive even though it doesn't have an associated
                // editor.
                documentCookie = RegisterAndReadLockDocument(moniker, vsTextBuffer);

                // Open the text buffer in a new window.  NOTE: The reason we need a hierarchy and an
                // itemId is that the IVsUIShellOpenDocument will search for the project that can
                // open the document unless we tell it the context (miscellaneous files).
                punkTextBuffer = Marshal.GetIUnknownForObject(vsTextBuffer);

                IVsWindowFrame vsWindowFrame;
                var editorType = editorTypeGuid ?? VSConstants.GUID_TextEditorFactory;
                var logicalView = logicalViewGuid ?? VSConstants.LOGVIEWID.TextView_guid;
                var hr = uiShellOpenDocument.OpenSpecificEditor(
                    grfOpenSpecific: 0,
                    pszMkDocument: moniker,
                    rguidEditorType: editorType,
                    pszPhysicalView: "Code",
                    rguidLogicalView: logicalView,
                    pszOwnerCaption: name,
                    pHier: vsUiHierarchy,
                    itemid: itemId,
                    punkDocDataExisting: punkTextBuffer,
                    pSPHierContext: _oleServiceProvider,
                    ppWindowFrame: out vsWindowFrame);
                ErrorHandler.ThrowOnFailure(hr);

                var vsRunningDocumentTable = _vsServiceProvider.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>();
                vsRunningDocumentTable.ModifyDocumentFlags(
                    documentCookie,
                    (uint)_VSRDTFLAGS.RDT_DontAddToMRU | (uint)_VSRDTFLAGS.RDT_CantSave, fSet: 1);

                if (vsWindowFrame != null)
                {
                    vsWindowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, true);
                    vsWindowFrame.Show();
                }

                return true;
            }
            catch (Exception ex)
            {
                _protectedOperations.Report(ex);
                return false;
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
        /// Create an IVsTextBuffer instance and populate it with the existing ITextBuffer value
        /// </summary>
        private IVsTextBuffer CreateVsTextBuffer(ITextBuffer textBuffer, string name)
        {
            // Wrap the result's ITextBuffer so that it can be used with the old VS APIs
            var vsTextBuffer = _vsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(_oleServiceProvider, textBuffer.ContentType);
            var textDocument = _textDocumentFactoryService.CreateTextDocument(textBuffer, name);

            // HACK OF EVIL HACKS: Since there is currently no way to create a text buffer shim
            // for an ITextBuffer that we have already created, we have to create an empty shim,
            // and initialize its underlying ITextBuffer ourselves via reflection
            var vsTextBufferType = vsTextBuffer.GetType();
            vsTextBufferType.GetField("_documentTextBuffer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vsTextBuffer, textBuffer);
            vsTextBufferType.GetField("_surfaceTextBuffer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vsTextBuffer, textBuffer);
            vsTextBufferType.GetField("_textDocument", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(vsTextBuffer, textDocument);
            vsTextBufferType.GetMethod("InitializeDocumentTextBuffer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(vsTextBuffer, null);

            return vsTextBuffer;
        }

        /// <summary>
        /// Create an IVsTextBuffer instance and populate it with the existing ITextBuffer value
        /// </summary>
        private IVsTextBuffer CreateVsTextBuffer(ITextBuffer textBuffer, string name, Guid? languageServiceGuid)
        {
            var vsTextBuffer = CreateVsTextBuffer(textBuffer, name);

            // If a language service was specified then attach it now
            if (languageServiceGuid.HasValue)
            {
                var id = languageServiceGuid.Value;
                ErrorHandler.ThrowOnFailure(vsTextBuffer.SetLanguageServiceID(ref id));
            }

            return vsTextBuffer;
        }

        private IVsTextView CreateVsTextView(IVsTextBuffer vsTextBuffer, params string[] textViewRoles)
        {
            // Set up the ITextView shim
            var textViewRoleSet = _textEditorFactoryService.CreateTextViewRoleSet(textViewRoles);
            var vsTextView = _vsEditorAdaptersFactoryService.CreateVsTextViewAdapter(_oleServiceProvider, textViewRoleSet);
            var hr = vsTextView.Initialize((IVsTextLines)vsTextBuffer, IntPtr.Zero, 0, null);
            ErrorHandler.ThrowOnFailure(hr);
            return vsTextView;
        }

        /// <summary>
        /// This will add a file to the misc folders without actually opening the document in 
        /// Visual Studio
        /// </summary>
        private void CreateHierarchy(string moniker, out IVsUIHierarchy vsUiHierarchy, out uint itemId)
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
            ErrorHandler.ThrowOnFailure(hr);

            // Get the hierarchy for the document we added to the miscellaneous files project
            IVsProject vsProject;
            hr = vsExternalFilesManager.GetExternalFilesProject(out vsProject);
            ErrorHandler.ThrowOnFailure(hr);

            int found;
            VSDOCUMENTPRIORITY[] priority = new VSDOCUMENTPRIORITY[1];
            hr = vsProject.IsDocumentInProject(moniker, out found, priority, out itemId);
            ErrorHandler.ThrowOnFailure(hr);
            if (0 == found || VSConstants.VSITEMID_NIL == itemId)
            {
                throw new Exception("Could not find in project");
            }

            vsUiHierarchy = (IVsUIHierarchy)vsProject;
        }

        private uint RegisterAndReadLockDocument(string moniker, IVsTextBuffer vsTextBuffer)
        {
            var runningDocumentTable = _vsServiceProvider.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>();

            uint documentCookie = VSConstants.VSCOOKIE_NIL;
            IntPtr punkDocData = IntPtr.Zero;
            try
            {
                punkDocData = Marshal.GetIUnknownForObject(vsTextBuffer);
                var hr = runningDocumentTable.RegisterAndLockDocument(
                    (uint)_VSRDTFLAGS.RDT_ReadLock,
                    moniker,
                    null,
                    VSConstants.VSITEMID_NIL,
                    punkDocData,
                    out documentCookie);
                ErrorHandler.ThrowOnFailure(hr);
                return documentCookie;
            }
            finally
            {
                if (punkDocData != IntPtr.Zero)
                {
                    Marshal.Release(punkDocData);
                }
            }
        }

        private void UnlockDocument(uint documentCookie)
        {
            var runningDocumentTable = _vsServiceProvider.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>();
            runningDocumentTable.UnlockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, documentCookie);
        }

        #region IEditorFactory

        IVsEditorFactory IEditorFactory.VsEditorFactory
        {
            get { return _vsEditorFactory; }
        }

        ITextBufferFactoryService IEditorFactory.TextBufferFactoryService
        {
            get { return _textBufferFactoryService; }
        }

        bool IEditorFactory.OpenInNewWindow(ITextBuffer textBuffer, string name, Guid? editorTypeId, Guid? logicalViewId, Guid? languageServiceId)
        {
            return OpenInNewWindow(textBuffer, name, editorTypeId, logicalViewId, languageServiceId);
        }

        IVsTextBuffer IEditorFactory.CreateVsTextBuffer(ITextBuffer textBuffer, string name)
        {
            return CreateVsTextBuffer(textBuffer, name);
        }

        IVsTextView IEditorFactory.CreateVsTextView(IVsTextBuffer vsTextBuffer, params string[] textViewRoles)
        {
            return CreateVsTextView(vsTextBuffer, textViewRoles);
        }

        #endregion
    }
}

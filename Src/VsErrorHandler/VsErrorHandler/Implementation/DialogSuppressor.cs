using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Settings;

namespace VsErrorHandler.Implementation
{
    /// <summary>
    /// This class exists to suppress the default Visual Studio Error Dialog.  It will do
    /// so as long as a window is open in Visual Studio
    /// </summary>
    [Name("Error Dialog Suppressor")]
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class DialogSuppressor : IWpfTextViewCreationListener
    {
        private readonly IOleServiceProvider _oleServiceProvider;
        private int _count;

        [ImportingConstructor]
        internal DialogSuppressor(SVsServiceProvider serviceProvider)
        {
            _oleServiceProvider = (IOleServiceProvider)serviceProvider.GetService(typeof(IOleServiceProvider));
        }

        private void ToggleSetting(bool enabled)
        {
            try
            {
                using (var serviceProvider = new ServiceProvider(_oleServiceProvider))
                {
                    var settingsManager = new ShellSettingsManager(serviceProvider);
                    var store = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                    store.SetBoolean("Text Editor", "Report Exceptions", enabled);
                }
            }
            catch
            {
                // The underlying COM objects can throw a variety of exceptions here
            }
        }

        void IWpfTextViewCreationListener.TextViewCreated(IWpfTextView textView)
        {
            if (_count == 0)
            {
                ToggleSetting(enabled: false);
            }

            _count++;
            textView.Closed += 
                (sender, e) =>
                {
                    _count--;
                    if (_count == 0)
                    {
                        ToggleSetting(enabled: true);
                    }
                };
        }

    }
}

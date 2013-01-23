using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IServiceProvider = System.IServiceProvider;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;

namespace ProjectionBufferDemo
{
    internal static class ExtensionMethods
    {
        internal static TInterface GetService<TService, TInterface>(this IServiceProvider serviceProvider)
        {
            return (TInterface)serviceProvider.GetService(typeof(TService));
        }

        internal static IOleServiceProvider GetOleServiceProvider(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<SDTE, IOleServiceProvider>();
        }

        internal static T GetExportedValue<T>(this IServiceProvider serviceProvider)
        {
            var componentModel = GetService<SComponentModel, IComponentModel>(serviceProvider);
            var export = componentModel.DefaultExportProvider;
            return export.GetExportedValue<T>();
        }

        internal static IServiceProvider GetServiceProvider(this IOleServiceProvider oleServiceProvider)
        {
            var serviceId = typeof(SComponentModel).GUID;
            var interfaceId = VSConstants.IID_IUnknown;
            var ptr = IntPtr.Zero;
            try
            {
                var hr = oleServiceProvider.QueryService(ref serviceId, ref interfaceId, out ptr);
                ErrorHandler.ThrowOnFailure(hr);

                var obj = Marshal.GetObjectForIUnknown(ptr);
                var componentModel = (IComponentModel)obj;
                return componentModel.DefaultExportProvider.GetExportedValue<SVsServiceProvider>();
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.Release(ptr);
                }
            }
        }
    }
}

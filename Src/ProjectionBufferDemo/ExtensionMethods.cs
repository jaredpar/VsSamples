using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectionBufferDemo
{
    internal static class ExtensionMethods
    {
        public static TInterface GetService<TService, TInterface>(this IServiceProvider serviceProvider)
        {
            return (TInterface)serviceProvider.GetService(typeof(TService));
        }
    }
}

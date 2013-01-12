using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JaredPar.ProjectionBufferDemo
{
    internal interface IEditorFactory
    {
        IVsEditorFactory CreateVsEditorFactory();
    }
}

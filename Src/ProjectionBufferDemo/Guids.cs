// Guids.cs
// MUST match guids.h
using System;

namespace JaredPar.ProjectionBufferDemo
{
    static class GuidList
    {
        public const string guidProjectionBufferDemoPkgString = "0d6c3e4c-e607-43b5-8ce7-8a6c7f07127d";
        public const string guidProjectionBufferDemoCmdSetString = "1c4dda70-5d74-4ac8-9619-84fe10636305";
        public const string guidToolWindowPersistanceString = "9deee938-cb7c-4f5a-90f4-6c70d3ac07bf";

        public static readonly Guid guidProjectionBufferDemoCmdSet = new Guid(guidProjectionBufferDemoCmdSetString);
    };
}
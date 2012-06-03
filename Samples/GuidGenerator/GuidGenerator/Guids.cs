// Guids.cs
// MUST match guids.h
using System;

namespace VsSamples.GuidGenerator
{
    static class GuidList
    {
        public const string guidGuidGeneratorPkgString = "d673ba8f-ca2e-48ec-a3df-6fb2b579cec5";
        public const string guidGuidGeneratorCmdSetString = "c3fbf294-5e5e-4a62-8efb-084764faa22c";

        public static readonly Guid guidGuidGeneratorCmdSet = new Guid(guidGuidGeneratorCmdSetString);
    };
}
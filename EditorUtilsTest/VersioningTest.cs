using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using System.Reflection;
using System.ComponentModel.Composition;

namespace EditorUtils.UnitTest
{
    [TestFixture]
    public sealed class VersioningTest
    {
        /// <summary>
        /// Get all type defined in the system
        /// </summary>
        private List<Type> GetAllTypes()
        {
            var list = new List<Type>();
            var seen = new HashSet<Type>();
            var toVisit = new Stack<Type>(typeof(ITaggerFactory).Assembly.GetTypes());
            while (toVisit.Count > 0)
            {
                var current = toVisit.Pop();
                if (!seen.Add(current))
                {
                    continue;
                }

                list.Add(current);
                foreach (var cur in current.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    toVisit.Push(cur);
                }
            }

            return list;
        }

        /// <summary>
        /// Make sure that every Export in the system uses ContractName.  Because MEF doesn't use 
        /// assembly qualified type names for matching we have to use the ContractName to create 
        /// versioning
        /// </summary>
        [Test]
        public void ExportMustHaveContractName()
        {
            var assembly = typeof(ITaggerFactory).Assembly;
            foreach (var cur in GetAllTypes())
            {
                var all = cur
                    .GetCustomAttributes(typeof(ExportAttribute), false)
                    .Cast<ExportAttribute>()
                    .Where(x => x.ContractType.Assembly == assembly);
                foreach (var attr in all)
                {
                    Assert.AreEqual(Constants.ContractName, attr.ContractName, String.Format("{0} needs a Export::ContractName for {1}", cur, attr.ContractType));
                }
            }
        }
    }
}

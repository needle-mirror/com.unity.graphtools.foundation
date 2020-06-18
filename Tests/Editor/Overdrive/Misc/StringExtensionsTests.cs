using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Misc
{
    [TestFixture]
    class StringExtensionsTests
    {
        [TestCase("AUpperCamelCaseString", "A Upper Camel Case String")]
        [TestCase("aLowerCamelCaseString", "A Lower Camel Case String")]
        public void NificyTest(string value, string expected)
        {
            Assert.That(value.Nicify(), Is.EqualTo(expected));
        }
    }
}

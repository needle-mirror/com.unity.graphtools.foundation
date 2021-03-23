using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Misc
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

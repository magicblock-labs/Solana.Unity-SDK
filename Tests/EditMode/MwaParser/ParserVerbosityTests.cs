#if !MWA_VERBOSE
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;
using UnityEngine.TestTools;

namespace SolanaMobileStack.Tests.EditMode
{
    public class ParserVerbosityTests
    {
        private static string FixturesDir =>
            Path.Combine(Path.GetFullPath("Packages/com.solana.unity_sdk"), "Tests", "EditMode", "MwaParser", "Fixtures");

        private static JToken LoadFixture(string name) =>
            JToken.Parse(File.ReadAllText(Path.Combine(FixturesDir, name)));

        [TearDown]
        public void TearDown() => LogAssert.NoUnexpectedReceived();

        [Test]
        public void Parse_WithVerbosityVerbose_EmitsStructuredLog()
        {
            LogAssert.Expect(LogType.Log, new Regex(@"^\[MWA parse\] present="));

            AuthorizationResponseParser.Parse(
                LoadFixture("authorize-v2-full.json"), LogVerbosity.Verbose);
        }

        [Test]
        public void Parse_WithVerbosityRelease_DoesNotEmitStructuredLog()
        {
            bool structuredLogFired = false;
            void handler(string msg, string stack, LogType type)
            {
                if (type == LogType.Log && msg.StartsWith("[MWA parse]"))
                    structuredLogFired = true;
            }

            Application.logMessageReceived += handler;
            try
            {
                AuthorizationResponseParser.Parse(
                    LoadFixture("authorize-v2-full.json"), LogVerbosity.Release);
            }
            finally
            {
                Application.logMessageReceived -= handler;
            }

            Assert.That(structuredLogFired, Is.False,
                "Expected no [MWA parse] structured log with LogVerbosity.Release");
        }
    }
}
#endif

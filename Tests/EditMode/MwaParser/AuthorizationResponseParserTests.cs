using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Solana.Unity.SolanaMobileStack;
using UnityEngine;

namespace SolanaMobileStack.Tests.EditMode
{
    public class AuthorizationResponseParserTests
    {
        private static string FixturesDir =>
            Path.Combine(Path.GetFullPath("Packages/com.solana.unity_sdk"), "Tests", "EditMode", "MwaParser", "Fixtures");

        private static JToken LoadFixture(string name) =>
            JToken.Parse(File.ReadAllText(Path.Combine(FixturesDir, name)));

        // ============ v2 happy path ============

        [Test]
        public void V2Full_Parses()
        {
            var result = AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-full.json"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.AuthToken, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Accounts, Is.Not.Null);
            Assert.That(result.Accounts.Count, Is.EqualTo(1));

            var bytes = result.PrimaryAccountPublicKeyBytes();
            Assert.That(bytes.Length, Is.EqualTo(32));

            var primary = result.PrimaryAccount();
            Assert.That(primary.DisplayAddress, Is.Not.Null.And.Not.Empty);
            Assert.That(primary.DisplayAddressFormat, Is.EqualTo("base58"));
            Assert.That(primary.Label, Is.Not.Null.And.Not.Empty);
            Assert.That(primary.Icon, Is.Not.Null.And.Not.Empty);
            Assert.That(primary.Chains, Is.Not.Null.And.Not.Empty);
            Assert.That(primary.Features, Is.Not.Null.And.Not.Empty);

            Assert.That(result.WalletUriBase, Is.Not.Null.And.Not.Empty);
            Assert.That(result.WalletIcon, Is.Not.Null.And.Not.Empty);
        }

        // ============ missing optionals ============

        [Test]
        public void V2Minimal_Parses()
        {
            var result = AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-minimal.json"));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.AuthToken, Is.EqualTo("auth-token-minimal"));
            Assert.That(result.Accounts.Count, Is.EqualTo(1));

            var primary = result.PrimaryAccount();
            Assert.That(primary.DisplayAddress, Is.Null);
            Assert.That(primary.DisplayAddressFormat, Is.Null);
            Assert.That(primary.Label, Is.Null);
            Assert.That(primary.Icon, Is.Null);
            Assert.That(primary.Chains, Is.Null);
            Assert.That(primary.Features, Is.Null);

            Assert.That(result.WalletUriBase, Is.Null);
            Assert.That(result.WalletIcon, Is.Null);
        }

        // ============ rejection cases ============

        [Test]
        public void V2EmptyAccounts_Throws()
        {
            Assert.Throws<InvalidAuthorizationException>(() =>
                AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-empty-accounts.json")));
        }

        [Test]
        public void V2BadBase64_Throws()
        {
            Assert.Throws<InvalidAuthorizationException>(() =>
                AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-bad-base64.json")));
        }

        [Test]
        public void V2WrongLength_Throws()
        {
            Assert.Throws<InvalidAuthorizationException>(() =>
                AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-wrong-length.json")));
        }

        [Test]
        public void V2EmptyAuthToken_Throws()
        {
            Assert.Throws<InvalidAuthorizationException>(() =>
                AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-empty-authtoken.json")));
        }

        // ============ size-cap drops ============

        [Test]
        public void V2OversizeIcon_DropsField()
        {
            var result = AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-oversize-icon.json"));
            Assert.That(result.Accounts[0].Icon, Is.Null,
                "icon > 64 KB post-decode must be dropped to null");
        }

        [Test]
        public void V2OversizeDisplay_DropsField()
        {
            var result = AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-oversize-display.json"));
            Assert.That(result.Accounts[0].DisplayAddress, Is.Null,
                "display_address > 128 chars must be dropped to null");
        }

        [Test]
        public void V2OversizeChains_Truncates()
        {
            var result = AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-oversize-chains.json"));
            Assert.That(result.Accounts[0].Chains.Length, Is.EqualTo(32),
                "chains[] > 32 entries must be truncated to first 32");
        }

        [Test]
        public void V2OversizeAccounts_Throws()
        {
            Assert.Throws<InvalidAuthorizationException>(() =>
                AuthorizationResponseParser.Parse(LoadFixture("authorize-v2-oversize-accounts.json")),
                "accounts[] > 16 must be rejected as malformed");
        }
    }
}

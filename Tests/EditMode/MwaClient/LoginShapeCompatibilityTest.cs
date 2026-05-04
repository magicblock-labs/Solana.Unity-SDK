using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;

namespace SolanaMobileStack.Tests.EditMode
{
    public class LoginShapeCompatibilityTest
    {
        [Test]
        public void Login_PublicSignatureUnchanged()
        {
            var method = typeof(SolanaMobileWalletAdapter).GetMethod(
                "_Login",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "_Login method must exist");
            Assert.That(method.IsFamily, Is.True, "_Login must be protected");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(Task<Account>)),
                "_Login must return Task<Account>");

            var parameters = method.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(1), "_Login must take exactly one parameter");
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)),
                "_Login parameter must be of type string");
            Assert.That(parameters[0].Name, Is.EqualTo("password"),
                "_Login parameter must be named 'password'");
            Assert.That(parameters[0].HasDefaultValue, Is.True,
                "_Login parameter must have a default value");
            Assert.That(parameters[0].DefaultValue, Is.Null,
                "_Login parameter default value must be null");
        }
    }
}

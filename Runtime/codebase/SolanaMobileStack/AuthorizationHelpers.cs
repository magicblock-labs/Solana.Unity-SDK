using System;

namespace Solana.Unity.SolanaMobileStack
{
    public static class AuthorizationHelpers
    {
        private const int PublicKeyByteLength = 32;

        public static AccountInfo PrimaryAccount(this AuthorizationResult authorization)
        {
            if (authorization == null) throw new ArgumentNullException(nameof(authorization));
            if (authorization.Accounts == null || authorization.Accounts.Count == 0)
                throw new InvalidAuthorizationException("AuthorizationResult has no accounts");
            return authorization.Accounts[0];
        }

        public static byte[] PrimaryAccountPublicKeyBytes(this AuthorizationResult authorization)
        {
            AccountInfo primary = authorization.PrimaryAccount();
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(primary.Address);
            }
            catch (FormatException fx)
            {
                throw new InvalidAuthorizationException(
                    "accounts[0].address is not valid base64", fx);
            }
            if (bytes.Length != PublicKeyByteLength)
                throw new InvalidAuthorizationException(
                    $"accounts[0].address decodes to {bytes.Length} bytes; expected {PublicKeyByteLength}");
            return bytes;
        }
    }
}

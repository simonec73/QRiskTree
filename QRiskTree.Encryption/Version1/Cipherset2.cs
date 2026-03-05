using System.Security.Cryptography;

namespace QRiskTree.Encryption.Version1
{
    internal class Cipherset2 : ICipherset
    {
        public int Iterations => 1000000;

        public int KeySize => 32;

        public int HashSize => 48;

        public byte[] Salt => RandomNumberGenerator.GetBytes(32);

        public HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA3_384;

        public SymmetricAlgorithm GetSymmetricAlgorithm()
        {
            SymmetricAlgorithm result = Aes.Create();
            result.KeySize = 256;
#pragma warning disable SCS0013 // Potential usage of weak CipherMode.
            // This does not constitute a security vulnerability as <see cref="EncryptionManager.Encrypt"/> adds an HMAC at the end of the binary stream.
            // Cfr https://security-code-scan.github.io/#SCS0013 for details on the finding.
            result.Mode = CipherMode.CBC;
#pragma warning restore SCS0013 // Potential usage of weak CipherMode.
            result.Padding = PaddingMode.PKCS7;

            return result;
        }

        public HMAC GetHMAC()
        {
            return new HMACSHA3_384();
        }
    }
}

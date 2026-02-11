using System.Security.Cryptography;

namespace QRiskTree.Encryption.Version1
{
    internal class Cipherset1 : ICipherset
    {
        public int Iterations => 200000;

        public int KeySize => 32;

        public int HashSize => 32;

        public byte[] Salt => RandomNumberGenerator.GetBytes(16);

        public HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA256;

        public SymmetricAlgorithm GetSymmetricAlgorithm()
        {
            SymmetricAlgorithm result = Aes.Create();
            result.KeySize = 256;
            result.Mode = CipherMode.CBC;
            result.Padding = PaddingMode.PKCS7;

            return result;
        }

        public HMAC GetHMAC()
        {
            return new HMACSHA256();
        }
    }
}

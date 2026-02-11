using System.Security.Cryptography;

namespace QRiskTree.Encryption
{
    internal interface ICipherset
    {
        int Iterations { get; }

        int KeySize { get; }

        int HashSize { get; }

        byte[] Salt { get; }

        HashAlgorithmName HashAlgorithm { get; }

        SymmetricAlgorithm GetSymmetricAlgorithm();

        HMAC GetHMAC();

    }
}

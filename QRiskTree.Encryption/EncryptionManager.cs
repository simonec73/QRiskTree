using QRiskTree.Encryption.Version1;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;

namespace QRiskTree.Encryption
{
    /// <summary>
    /// Class incapsulating the logic to encrypt and decrypt messages using a passphrase and a cipherset.
    /// </summary>
    public class EncryptionManager
    {
        private SecureString? _passphrase;
        private readonly ICipherset _c1 = new Cipherset1();
        private readonly ICipherset _c2 = new Cipherset2();
        private int _iterations;
        private byte[] _salt = Array.Empty<byte>();
        private int _keySize;
        private int _hashSize;
        private HashAlgorithmName _hashAlgorithm;

        #region Public members.
        /// <summary>
        /// Default cipherset to use for encryption and decryption. 
        /// The default cipherset is the most secure one available in the library, 
        /// but it can be changed to a less secure one for compatibility reasons.
        /// </summary>
        public const short DefaultCipherSet = 2;

        /// <summary>
        /// Flag indicating whether the encryption manager is initialized with a passphrase or not.
        /// </summary>
        public bool IsInitialized => _passphrase != null;

        /// <summary>
        /// Sets the passphrase to use for encryption and decryption.
        /// </summary>
        /// <param name="passphrase">The passphrase to use for encryption and decryption.</param>
        public void SetPassphrase(SecureString passphrase)
        {
            _passphrase = passphrase;
        }

        /// <summary>
        /// Resets the passphrase, making the encryption manager uninitialized.
        /// </summary>
        public void ResetPassphrase()
        {
            _passphrase = null;
        }

        /// <summary>
        /// Encrypts the given data using the passphrase and the specified cipherset.
        /// </summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="cipherSet">The cipherset to use for encryption.
        /// If missing, the latest and strongest cipherset will be used.</param>
        /// <returns>The encrypted data.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the passphrase is not set.</exception>
        public byte[] Encrypt(byte[] data, short cipherSet = DefaultCipherSet)
        {
            if (_passphrase == null)
                throw new InvalidOperationException("Passphrase not set.");

            _iterations = GetIterations(cipherSet);
            _salt = GetSalt(cipherSet);
            _keySize = GetKeySize(cipherSet);
            _hashSize = GetHashSize(cipherSet);
            _hashAlgorithm = GetHashAlgorithm(cipherSet);

            using (var algorithm = GetAlgorithm(_passphrase, cipherSet))
            {
                algorithm.GenerateIV();
                var iv = algorithm.IV;

                byte[] encrypted;

                using (var encryptor = algorithm.CreateEncryptor(algorithm.Key, iv))
                using (var cipherStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(cipherStream, encryptor, CryptoStreamMode.Write))
                    using (var writer = new BinaryWriter(cryptoStream))
                    {
                        writer.Write(data);
                        cryptoStream.FlushFinalBlock();
                        encrypted = cipherStream.ToArray();
                    }
                }

                // The result is obtained by concatenating the IV and the ciphertext.
                var payload = new byte[sizeof(short) + sizeof(short) + _salt.Length + sizeof(short) + iv.Length + sizeof(int) + encrypted.Length];
                Array.Copy(Convert(cipherSet), 0, payload, 0, sizeof(short));
                Array.Copy(Convert((short)_salt.Length), 0, payload, sizeof(short), sizeof(short));
                Array.Copy(_salt, 0, payload, 2* sizeof(short), _salt.Length);
                Array.Copy(Convert((short)iv.Length), 0, payload, 2 * sizeof(short) + _salt.Length, sizeof(short));
                Array.Copy(iv, 0, payload, 3 * sizeof(short) + _salt.Length, iv.Length);
                Array.Copy(Convert(encrypted.Length), 0, payload, 3 * sizeof(short) + _salt.Length + iv.Length, sizeof(int));
                Array.Copy(encrypted, 0, payload, 3 * sizeof(short) + _salt.Length + iv.Length + sizeof(int), encrypted.Length);

                byte[] result;
                using (var hmac = GetHMAC(_passphrase, cipherSet))
                {
                    var hash = hmac.ComputeHash(payload);
                    result = new byte[payload.Length + hash.Length + sizeof(short)];
                    Array.Copy(payload, result, payload.Length);
                    Array.Copy(Convert((short)hash.Length), 0, result, payload.Length, sizeof(short));
                    Array.Copy(hash, 0, result, payload.Length + sizeof(short), hash.Length);
                }

                return result;
            }
        }

        /// <summary>
        /// Decrypts the specified encrypted data using the configured passphrase and cryptographic parameters.
        /// </summary>
        /// <param name="encryptedData">The encrypted data to decrypt.</param>
        /// <returns>The decrypted data as a byte array, or null if decryption fails.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the passphrase is not set.</exception>
        /// <exception cref="NotSupportedException">Thrown when the cipher set specified in the encrypted data is invalid or not supported.</exception>
        /// <exception cref="CryptographicException">Thrown when the data integrity check fails.</exception>
        public byte[]? Decrypt(byte[] encryptedData)
        {
            if (_passphrase == null)
                throw new InvalidOperationException("Passphrase not set.");

            byte[]? result = null;

            var start = 0;
            var end = 2;
            var cipherSet = (short) ConvertToShort(encryptedData[start..end]);
            if (cipherSet < 0 || cipherSet > DefaultCipherSet)
                throw new NotSupportedException("Invalid cipher set.");

            _iterations = GetIterations(cipherSet);
            _keySize = GetKeySize(cipherSet);
            _hashSize = GetHashSize(cipherSet);
            _hashAlgorithm = GetHashAlgorithm(cipherSet);

            ReadShortLength(encryptedData, ref start, ref end);
            _salt = encryptedData[start..end];
            ReadShortLength(encryptedData, ref start, ref end);
            var iv = encryptedData[start..end];
            ReadIntLength(encryptedData, ref start, ref end);
            var encrypted = encryptedData[start..end];
            var payload = encryptedData[0..end];
            ReadShortLength(encryptedData, ref start, ref end);
            var hash = encryptedData[start..end];

            using (var hmac = GetHMAC(_passphrase, cipherSet))
            {
                var computedHash = hmac.ComputeHash(payload);
                if (!Equal(hash, computedHash))
                    throw new CryptographicException("Data integrity check failed.");
            }

            using (var algorithm = GetAlgorithm(_passphrase, cipherSet))
            {
                algorithm.IV = iv;

                using (var stream = new MemoryStream())
                {
                    using (var cryptoStream =
                        new CryptoStream(stream, algorithm.CreateDecryptor(), CryptoStreamMode.Write))
                    using (var writer = new BinaryWriter(cryptoStream))
                    {
                        writer.Write(encrypted);
                    }

                    result = stream.ToArray();
                }
            }

            return result;
        }
        #endregion

        #region Private members to handle the cipherset details.
        private int GetIterations(short cipherSet)
        {
            return cipherSet switch
            {
                1 => _c1.Iterations,
                _ => _c2.Iterations
            };
        }

        private int GetKeySize(short cipherSet)
        {
            return cipherSet switch
            {
                1 => _c1.KeySize,
                _ => _c2.KeySize
            };
        }

        private int GetHashSize(short cipherSet)
        {
            return cipherSet switch
            {
                1 => _c1.HashSize,
                _ => _c2.HashSize
            };
        }

        private byte[] GetSalt(short cipherSet)
        {
            return cipherSet switch
            {
                1 => _c1.Salt,
                _ => _c2.Salt
            };

        }

        private HashAlgorithmName GetHashAlgorithm(short cipherSet)
        {
            return cipherSet switch
            {
                1 => _c1.HashAlgorithm,
                _ => _c2.HashAlgorithm
            };
        }

        private SymmetricAlgorithm GetAlgorithm(SecureString passphrase, short cipherSet)
        {
            SymmetricAlgorithm result = cipherSet switch
            {
                1 => _c1.GetSymmetricAlgorithm(),
                _ => _c2.GetSymmetricAlgorithm()
            };

            result.Key = passphrase.Process(DeriveKey);

            return result;
        }

        private HMAC GetHMAC(SecureString passphrase, short cipherSet)
        {
            HMAC result = cipherSet switch
            {
                1 => _c1.GetHMAC(),
                _ => _c2.GetHMAC()
            };

            result.Key = passphrase.Process(DeriveHash);

            return result;
        }
        #endregion

        #region Private members to handle the byte conversions and other utilities.
        private byte[] DeriveKey(byte[] password)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, _salt, _iterations, _hashAlgorithm, _keySize);
        }

        private byte[] DeriveHash(byte[] password)
        {
            return Rfc2898DeriveBytes.Pbkdf2(password, _salt, _iterations, _hashAlgorithm, _hashSize);
        }

        private byte[] Convert(int number)
        {
            var result = new byte[4];

            result[0] = (byte)(number >> 24);
            result[1] = (byte)(number >> 16);
            result[2] = (byte)(number >> 8);
            result[3] = (byte)number;

            return result;
        }

        private byte[] Convert(short number)
        {
            var result = new byte[2];

            result[0] = (byte)(number >> 8);
            result[1] = (byte)number;

            return result;
        }

        private int ConvertToInt(byte[] number)
        {
            return (number[0] << 24) + (number[1] << 16) + (number[2] << 8) + number[3];
        }

        private int ConvertToShort(byte[] number)
        {
            return (number[0] << 8) + number[1];
        }

        private bool Equal([NotNull] byte[] source, [NotNull] byte[] target)
        {
            var result = true;
            if (source.Length == target.Length)
            {
                for (var i = 0; i < source.Length; i++)
                {
                    if (source[i] != target[i])
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;
            }

            return result;
        }

        private void ReadShortLength(byte[] source, ref int start, ref int end)
        {
            start = end;
            end += 2;
            var result = ConvertToShort(source[start..end]);
            start = end;
            end += result;
        }

        private void ReadIntLength(byte[] source, ref int start, ref int end)
        {
            start = end;
            end += 4;
            var result = ConvertToInt(source[start..end]);
            start = end;
            end += result;
        }
        #endregion
    }
}

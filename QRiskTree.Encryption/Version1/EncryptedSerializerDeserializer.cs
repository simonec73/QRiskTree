using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace QRiskTree.Encryption.Version1
{
    internal sealed class EncryptedSerializerDeserializer<T> : IEncryptedSerializerDeserializer<T> where T : class
    {
        public EncryptedSerializerDeserializer()
        {
        }

        public const byte TYPE = 0x01;

        public byte TypeId => TYPE;

        public void WriteTo(T input, BinaryWriter w, EncryptionManager encryptionManager)
        {
            if (encryptionManager == null)
                throw new InvalidOperationException("Encryption manager is not initialized.");

            var serializedModel = ToBson(input);
            var encryptedPayload = encryptionManager.Encrypt(serializedModel);

            var len = (uint)encryptedPayload.Length;
            w.Write(len);
            w.Write(encryptedPayload);
        }

        public T ReadFrom(BinaryReader r, EncryptionManager encryptionManager)
        {
            if (encryptionManager == null)
                throw new InvalidOperationException("Encryption manager is not initialized.");

            var len = r.ReadUInt32();
            var payload = r.ReadBytes((int)len);

            var decryptedPayload = encryptionManager.Decrypt(payload);

            if (decryptedPayload == null)
                throw new InvalidOperationException("Decryption failed");

            return FromBson(decryptedPayload);
        }

        private static byte[] ToBson(T value)
        {
            using var ms = new MemoryStream();
            using (var writer = new BsonDataWriter(ms))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, value);
            }
            return ms.ToArray();
        }

        private static T FromBson(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using (var reader = new BsonDataReader(ms))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<T>(reader)!;
            }
        }
    }
}
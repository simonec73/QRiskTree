using QRiskTree.Encryption.Version1;
using System.Text;

namespace QRiskTree.Encryption
{

    /// <summary>
    /// Implements the logic to write and read encrypted messages.
    /// </summary>
    public class BinaryProtocol<T> where T : class
    {
        private const byte Magic1 = (byte)'Q';
        private const byte Magic2 = (byte)'R';
        private readonly SerializersDeserializersRegistry<T> _registry = new SerializersDeserializersRegistry<T>();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BinaryProtocol()
        {
            _registry.Register(typeof(EncryptedSerializerDeserializer<T>));
        }

        /// <summary>
        /// Serializes a Risk Model on the output stream.
        /// </summary>
        /// <param name="model">The Risk Model to serialize.</param>
        /// <param name="output">The stream where the encrypted data must be written.</param>
        /// <param name="encryptionManager">The encryption manager to encrypt the message.</param>
        /// <exception cref="ArgumentNullException">A parameter is null.</exception>
        /// <exception cref="ArgumentException">The stream is not writable.</exception>
        public void Write(T model, Stream output, EncryptionManager encryptionManager)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));
            if (output is null) throw new ArgumentNullException(nameof(output));
            if (encryptionManager is null) throw new ArgumentNullException(nameof(encryptionManager));
            if (!output.CanWrite) throw new ArgumentException("The stream is not writable.", nameof(output));

            if (_registry.TryGetCurrent(out var serializer) && serializer != null)
            {
                using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
                writer.Write(Magic1);
                writer.Write(Magic2);
                writer.Write(serializer.TypeId);
                serializer.WriteTo(model, writer, encryptionManager);
                writer.Flush();
                writer.Close();
            }
        }

        /// <summary>
        /// Deserializes a message from the input stream.
        /// </summary>
        /// <param name="input">The stream containing the serialized encrypted Risk Model.</param>
        /// <param name="encryptionManager">The encryption manager to decrypt the message.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="ArgumentNullException">The input stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="InvalidDataException">Deserialization failed.</exception>
        public T? Read(Stream input, EncryptionManager encryptionManager)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (!input.CanRead) throw new ArgumentException("The stream is not readable.", nameof(input));

            T? result = null;

            using (var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true))
            {
                var m1 = reader.ReadByte();
                var m2 = reader.ReadByte();
                if (m1 != Magic1 || m2 != Magic2)
                {
                    throw new InvalidDataException(
                        $"Invalid header. Expected '{(char)Magic1}{(char)Magic2}', found '{(char)m1}{(char)m2}'.");
                }

                var typeId = reader.ReadByte();
                if (_registry.TryGet(typeId, out var deserializer) && deserializer != null)
                {
                    result = deserializer.ReadFrom(reader, encryptionManager);
                }
                else
                {
                    throw new InvalidDataException($"Deserialization failed.");
                }
            }

            return result;
        }
    }
}
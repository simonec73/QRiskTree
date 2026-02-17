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
        private byte _magic1 = Magic1;
        private byte _magic2 = Magic2;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BinaryProtocol()
        {
            _registry.Register(typeof(EncryptedSerializerDeserializer<T>));
        }

        /// <summary>
        /// Overrides the default magic characters used in the protocol to ensure that the header of the message is correct. 
        /// This can help prevent accidental processing of messages that do not conform with the expected format.
        /// </summary>
        /// <param name="magic1">The first magic character.</param>
        /// <param name="magic2">The second magic character.</param>
        /// <exception cref="ArgumentException">Thrown if any magic character is zero.</exception>
        /// <remarks>This is not thread safe, but it is expected to be called only once at the beginning of the application.</remarks>
        public void OverrideMagicChars(byte magic1, byte magic2)
        {
            if (magic1 == 0) throw new ArgumentException("Magic character cannot be zero.", nameof(magic1));
            if (magic2 == 0) throw new ArgumentException("Magic character cannot be zero.", nameof(magic2));
            _magic1 = magic1;
            _magic2 = magic2;
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
                writer.Write(_magic1);
                writer.Write(_magic2);
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
                if (m1 != _magic1 || m2 != _magic2)
                {
                    throw new InvalidDataException(
                        $"Invalid header. Expected '{(char)_magic1}{(char)_magic2}', found '{(char)m1}{(char)m2}'.");
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
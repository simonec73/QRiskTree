namespace QRiskTree.Encryption
{
    /// <summary>
    /// Contract used for the serializers/deserializers of encrypted Risk Models.
    /// </summary>
    internal interface IEncryptedSerializerDeserializer<T> where T : class
    {
        /// <summary>Identifier of the type of the message.</summary>
        byte TypeId { get; }

        /// <summary>
        /// Writes the content of the input object.
        /// </summary>
        /// <param name="model">The object to serialize.</param>
        /// <param name="writer">The BinaryWriter to write the serialized data.</param>
        /// <param name="encryptionManager">The encryption manager to use for encrypting the message.</param>
        void WriteTo(T input, BinaryWriter writer, EncryptionManager encryptionManager);

        /// <summary>
        /// Reads the content of the input object from the BinaryReader and returns it.
        /// </summary>
        /// <param name="reader">The BinaryReader containing the serialized input object.</param>
        /// <param name="encryptionManager">The encryption manager to use for decrypting the message.</param>
        /// <returns>The deserialized object.</returns>
        T ReadFrom(BinaryReader reader, EncryptionManager encryptionManager);
    }
}
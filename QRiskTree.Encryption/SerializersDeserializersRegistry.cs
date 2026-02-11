namespace QRiskTree.Encryption
{
    /// <summary>
    /// Registry for dynamically mapping the typeId with the serializers/deserializers.
    /// </summary>
    internal class SerializersDeserializersRegistry<T> where T : class
    {
        private readonly Dictionary<byte, Type> _serializersDeserializers = new();
        private readonly object _lock = new();

        /// <summary>
        /// Registers the factory linked with a typeId.
        /// </summary>
        /// <param name="deserializerType">Type containing the factory method to create the deserializer.</param>
        /// <exception cref="ArgumentNullException">The factory method is not defined.</exception>
        /// <exception cref="InvalidOperationException">The typeId is already registered.</exception>
        public void Register(Type deserializerType)
        {
            if (deserializerType is null) throw new ArgumentNullException(nameof(deserializerType));
            if (!deserializerType.GetInterfaces().Any(i => i == typeof(IEncryptedSerializerDeserializer<T>)))
                throw new InvalidOperationException($"The type {deserializerType.FullName} does not implement IEncryptedSerializerDeserializer.");

            lock (_lock)
            {
                var typeIdField = deserializerType.GetField("TYPE", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (typeIdField == null)
                    throw new InvalidOperationException($"The type {deserializerType.FullName} does not define a public static TYPE field.");

                var typeId = (byte)typeIdField.GetValue(null)!;

                if (_serializersDeserializers.ContainsKey(typeId))
                    throw new InvalidOperationException($"TypeId 0x{typeId:X2} is already registered.");

                _serializersDeserializers[typeId] = deserializerType;
            }
        }

        public bool TryGetCurrent(out IEncryptedSerializerDeserializer<T>? instance)
        {
            var result = false;
            instance = null;

            lock (_lock)
            {
                instance = _serializersDeserializers.MaxBy(x => x.Key).Value
                    .TypeInitializer?.Invoke(null, null) as IEncryptedSerializerDeserializer<T>;
                result = instance != null;
            }

            return result;
        }

        /// <summary>
        /// Try to get the serializer/deserializer for an encrypted stream given its typeId.
        /// </summary>
        /// <param name="typeId">Identifier of the type of the deserializer.</param>
        /// <param name="instance">[out] Encrypted serializer/deserializer.</param>
        /// <returns></returns>
        public bool TryGet(byte typeId, out IEncryptedSerializerDeserializer<T>? instance)
        {
            var result = false;
            instance = null;

            lock (_lock)
            {
                if (_serializersDeserializers.TryGetValue(typeId, out var type))
                {
                    instance = type.TypeInitializer?.Invoke(null, null) as IEncryptedSerializerDeserializer<T>;
                    result = instance != null;
                }
            }

            return result;
        }
    }
}
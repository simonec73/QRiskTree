using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QRiskTree.Engine.ExtendedOpenFAIR;
using QRiskTree.Engine.OpenFAIR;

namespace QRiskTree.Engine.Facts
{
    public class FactsTypesBinder : ISerializationBinder
    {
        private static readonly Dictionary<string, Type> _knownTypes = new Dictionary<string, Type>();

        static FactsTypesBinder()
        {
            AddKnownType(typeof(Fact));
            AddKnownType(typeof(FactHardNumber));
            AddKnownType(typeof(FactRange));
            AddKnownType(typeof(FactsCollection));
        }

        public static void AddKnownType(Type type)
        {
            var name = type.FullName;
            if (!string.IsNullOrWhiteSpace(name) && !_knownTypes.ContainsKey(name))
                _knownTypes.Add(name, type);
        }

        public Type BindToType(string? assemblyName, string typeName)
        {
            Type? result = null;

            try
            {
                var qualifiedTypeName = $"{assemblyName}#{typeName}";
                if (_knownTypes.ContainsKey(typeName))
                    result = _knownTypes[typeName];
                else
                {    
                    throw new JsonSerializationException($"Type '{typeName}' is not allowed for deserialization.");
                }
            }
            catch
            {
                result = null;
            }

            return result ?? typeof(object);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.Assembly?.FullName ?? string.Empty;
            typeName = serializedType.FullName ?? string.Empty;
        }
    }
}
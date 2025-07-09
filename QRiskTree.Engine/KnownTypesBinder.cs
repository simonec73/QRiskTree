using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;
using QRiskTree.Engine.Model;

namespace QRiskTree.Engine
{
    public class KnownTypesBinder : ISerializationBinder
    {
        private static readonly Dictionary<string, Type> _knownTypes = new Dictionary<string, Type>();

        static KnownTypesBinder()
        {
            AddKnownType(typeof(Fact));
            AddKnownType(typeof(FactHardNumber));
            AddKnownType(typeof(FactRange));
            AddKnownType(typeof(FactsManager));
            AddKnownType(typeof(ContactFrequency));
            AddKnownType(typeof(LossEventFrequency));
            AddKnownType(typeof(LossMagnitude));
            AddKnownType(typeof(PrimaryLoss));
            AddKnownType(typeof(ProbabilityOfAction));
            AddKnownType(typeof(ResistenceStrength));
            AddKnownType(typeof(Risk));
            AddKnownType(typeof(SecondaryLossEventFrequency));
            AddKnownType(typeof(SecondaryLossMagnitude));
            AddKnownType(typeof(SecondaryRisk));
            AddKnownType(typeof(ThreatCapability));
            AddKnownType(typeof(ThreatEventFrequency));
            AddKnownType(typeof(Vulnerability));
            AddKnownType(typeof(AppliedMitigation));
            AddKnownType(typeof(MitigatedRisk));
            AddKnownType(typeof(MitigationCost));
            AddKnownType(typeof(RiskModel));
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
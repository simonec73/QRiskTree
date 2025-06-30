using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QRiskTree.Engine.ExtendedOpenFAIR;
using QRiskTree.Engine.Facts;
using QRiskTree.Engine.OpenFAIR;

namespace QRiskTree.Engine
{
    public class KnownTypesBinder : ISerializationBinder
    {
        private static readonly Dictionary<string, Type> _knownTypes = new Dictionary<string, Type>();

        public static event Action<string?, string>? TypeNotFound;

        static KnownTypesBinder()
        {
            AddKnownType(typeof(Fact));
            AddKnownType(typeof(FactHardNumber));
            AddKnownType(typeof(FactRange));
            AddKnownType(typeof(FactsDictionary));
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

        public bool HasUnknownTypes { get; private set; }

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

//#if NETCOREAPP
//                    if (string.CompareOrdinal(assemblyName, "mscorlib") == 0)
//                    {
//                        assemblyName = "System.Private.CoreLib";
//                    }
//#else
//                    if (string.CompareOrdinal(assemblyName, "System.Private.CoreLib") == 0)
//                    {
//                        assemblyName = "mscorlib";
//                    }
//#endif

                    //if (!string.IsNullOrWhiteSpace(assemblyName))
                    //{
                    //    var assembly = Assembly.Load(assemblyName);
                    //    if (assembly != null)
                    //    {
                    //        result = GetGenericType(typeName, assembly);

                    //        if (result == null)
                    //        {
                    //            if (typeName.EndsWith("[]"))
                    //            {
                    //                int length = typeName.IndexOf('[');
                    //                if (length >= 0)
                    //                {
                    //                    string name = typeName.Substring(0, length);
                    //                    if (_knownTypes.ContainsKey(name))
                    //                    {
                    //                        var type = assembly.GetType(typeName);
                    //                        if (type != null)
                    //                        {
                    //                            result = type;
                    //                            _knownTypes.Add(typeName, result);
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                }
            }
            catch
            {
                result = null;
                HasUnknownTypes = true;
                TypeNotFound?.Invoke(assemblyName, typeName);
            }

            return result ?? typeof(object);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.Assembly?.FullName ?? string.Empty;
            typeName = serializedType.FullName ?? string.Empty;
        }

        //private Type GetGenericType(string typeName, Assembly assembly)
        //{
        //    Type result = null;

        //    int length = typeName.IndexOf('[');
        //    if (length >= 0)
        //    {
        //        string name = typeName.Substring(0, length);
        //        Type type = assembly.GetType(name);
        //        if (type?.IsGenericType ?? false)
        //        {
        //            var typeList = new List<Type>();
        //            int position = 0;
        //            int startIndex = 0;
        //            int last = typeName.Length - 1;
        //            for (int index = length + 1; index < last; ++index)
        //            {
        //                switch (typeName[index])
        //                {
        //                    case '[':
        //                        if (position == 0)
        //                            startIndex = index + 1;
        //                        position++;
        //                        break;
        //                    case ']':
        //                        position--;
        //                        if (position == 0)
        //                        {
        //                            var tn = typeName.Substring(startIndex, index - startIndex);
        //                            var split = tn.Split(',');
        //                            var innerTypeName = split[0].Trim();
        //                            var innerAssemblyName = split[1].Trim();
        //                            var innerQualifiedTypeName = $"{innerAssemblyName}#{innerTypeName}";
        //                            if (_knownTypes.ContainsKey(innerTypeName))
        //                            {
        //                                typeList.Add(_knownTypes[innerTypeName]);
        //                            }
        //                            else if (_equivalences.ContainsKey(innerQualifiedTypeName))
        //                            {
        //                                typeList.Add(_equivalences[innerQualifiedTypeName]);
        //                            }
        //                        }
        //                        break;
        //                }
        //            }

        //            result = type.MakeGenericType(typeList.ToArray());
        //        }
        //    }

        //    return result;
        //}
    }
}
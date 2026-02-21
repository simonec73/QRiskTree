using Cti.Stix;
using Cti.Stix.Core.SCO;
using Cti.Stix.Core.SDO;
using Cti.Stix.Core.SRO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace QRiskTree.Engine.ImportExport
{
    /// <summary>
    /// Importer for the Stix format. 
    /// This format is used for importing risk models from external sources that use the Stix standard for representing cyber threat information. The importer will parse the Stix file and convert it into the internal representation of the risk model used by the QRiskTree engine.
    /// </summary>
    public class StixImporter : IImporter
    {
        #region Nested classes for JSON deserialization.
        /// <summary>
        /// Custom JSON converter for STIX objects that uses the 'type' discriminator field
        /// </summary>
        private class StixConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(IStix);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                // Load the JSON object
                var jsonObject = Newtonsoft.Json.Linq.JObject.Load(reader);

                // Get the type discriminator
                var typeValue = jsonObject["type"]?.Value<string>();

                if (string.IsNullOrEmpty(typeValue))
                {
                    return null;
                }

                // Map STIX type strings to concrete classes
                var targetType = typeValue switch
                {
                    "artifact" => typeof(Artifact),
                    "attack-pattern" => typeof(AttackPattern),
                    "autonomous-system" => typeof(AutonomousSystem),
                    "campaign" => typeof(Campaign),
                    "course-of-action" => typeof(CourseOfAction),
                    "directory" => typeof(Cti.Stix.Core.SCO.Directory),
                    "domain-name" => typeof(DomainName),
                    "email-addr" => typeof(EmailAddress),
                    "email-message" => typeof(EmailMessage),
                    "file" => typeof(Cti.Stix.Core.SCO.File),
                    "grouping" => typeof(Grouping),
                    "identity" => typeof(Identity),
                    "incident" => typeof(Incident),
                    "indicator" => typeof(Indicator),
                    "infrastructure" => typeof(Infrastructure),
                    "intrusion-set" => typeof(IntrusionSet),
                    "ipv4-addr" => typeof(Ipv4),
                    "ipv6-addr" => typeof(Ipv6),
                    "location" => typeof(Location),
                    "mac-addr" => typeof(MacAddress),
                    "malware" => typeof(Malware),
                    "malware-analysis" => typeof(MalwareAnalysis),
                    "mutex" => typeof(Cti.Stix.Core.SCO.Mutex),
                    "network-traffic" => typeof(NetworkTraffic),
                    "note" => typeof(Note),
                    "observed-data" => typeof(ObservedData),
                    "opinion" => typeof(Opinion),
                    "process" => typeof(Process),
                    "relationship" => typeof(Relationship),
                    "Relationship" => typeof(Relationship),
                    "report" => typeof(Report),
                    "sighting" => typeof(Sighting),
                    "software" => typeof(Software),
                    "threat-actor" => typeof(ThreatActor),
                    "tool" => typeof(Tool),
                    "url" => typeof(Url),
                    "user-account" => typeof(UserAccount),
                    "vulnerability" => typeof(Vulnerability),
                    "windows-registry-key" => typeof(WindowsRegistryKey),
                    "x509-certificate" => typeof(X509Certificate),
                    _ => null
                };

                if (targetType == null)
                {
                    // Unknown type, skip it
                    return null;
                }

                // Deserialize to the correct type
                return jsonObject.ToObject(targetType, serializer);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value);
            }
        }
        #endregion

        public string FileDescription => "OASIS STIX";

        public string FileExtension => ".json";

        public bool TryImportingIntoRiskModel<R, M>(string filePath, ref IRiskModel<R, M> model)
            where R : INamedObject
            where M : INamedObject
        {
            if (model == null) return false;
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath)) return false;

            var json = System.IO.File.ReadAllText(filePath);

            // Configure JsonSerializerSettings to handle STIX type discrimination
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Converters = new List<JsonConverter> { new StixConverter() }
            };

            var bundle = JsonConvert.DeserializeObject<Bundle>(json, settings);
            if (bundle == null)
                return false;
            else
            {
                var risks = new Dictionary<string, R>();
                var attackPatterns = bundle.Objects.OfType<AttackPattern>().ToArray();
                foreach (var attackPattern in attackPatterns)
                {
                    var risk = model.AddRisk(attackPattern.Name ?? "Unnamed Risk");
                    var builder = new StringBuilder();
                    builder.AppendLine(attackPattern.Description);
                    var externalReferences = attackPattern.ExternalReferences?.ToArray();
                    if (externalReferences?.Any() ?? false)
                    {
                        foreach (var reference in externalReferences)
                        {
                            builder.AppendLine($"Reference: [{reference.SourceName}] {reference.ExternalID} - {reference.URL}");
                        }
                    }
                    risk.Description = builder.ToString();

                    risks[attackPattern.ID] = risk;
                }

                var mitigations = new Dictionary<string, M>();
                var courseOfActions = bundle.Objects.OfType<CourseOfAction>().ToArray();
                foreach (var courseOfAction in courseOfActions)
                {
                    var mitigation = model.AddMitigation(courseOfAction.Name ?? "Unnamed Mitigation");
                    var builder = new StringBuilder();
                    builder.AppendLine(courseOfAction.Description);
                    var externalReferences = courseOfAction.ExternalReferences?.ToArray();
                    if (externalReferences?.Any() ?? false)
                    {
                        foreach (var reference in externalReferences)
                        {
                            builder.AppendLine($"Reference: [{reference.SourceName}] {reference.ExternalID} - {reference.URL}");
                        }
                    }
                    mitigation.Description = builder.ToString();

                    mitigations[courseOfAction.ID] = mitigation;
                }

                var relationships = bundle.Objects.OfType<Relationship>().ToArray();
                foreach (var relationship in relationships)
                {
                    if (string.CompareOrdinal(relationship.RelationshipType, "mitigates") == 0)
                    {
                        if (mitigations.TryGetValue(relationship.SourceRef, out var source) &&
                            risks.TryGetValue(relationship.TargetRef, out var target) &&
                            target is IApplyMitigation<M> applyMitigationRisk)
                        {
                            applyMitigationRisk.ApplyMitigation(source);
                        }
                    }
                }

                var notes = bundle.Objects.OfType<Note>().ToArray();
                foreach (var note in notes)
                {
                    var source = string.Join(", ", note.Authors ?? Enumerable.Empty<string>());
                    var fact = model.AddFact("STIX Note", source, note.Abstract ?? "Unnamed Fact", note.Content);
                    if (fact != null)
                    {
                        var references = note.ObjectRefs?.ToArray();
                        if (references?.Any() ?? false)
                        {
                            foreach (var reference in references)
                            {
                                if (risks.TryGetValue(reference, out var risk) && risk is IFactContainer c1)
                                {
                                    c1.AddFact(fact);
                                }
                                else if (mitigations.TryGetValue(reference, out var mitigation) && mitigation is IFactContainer c2)
                                {
                                    c2.AddFact(fact);
                                }
                            }
                        }
                    }
                }

                return true;
            }
        }
    }
}

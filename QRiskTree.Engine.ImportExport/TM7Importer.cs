using System.Text;
using TMFileParser;
using TMFileParser.Models.output;

namespace QRiskTree.Engine.ImportExport
{
    /// <summary>
    /// Importer for Microsoft Threat Modeling Tool files (.tm7).
    /// </summary>
    public class TM7Importer : IImporter
    {
        public string FileDescription => "Microsoft Threat Modeling Tool files";

        public string FileExtension => ".tm7";

        public bool TryImportingIntoRiskModel<R, M>(string filePath, ref IRiskModel<R, M> model) where R : INamedObject where M : INamedObject
        {
            if (model == null) return false;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

            var reader = new TM7FileReader(new FileInfo(filePath));
            var threats = (IEnumerable<object>)reader.GetData("threats") as IEnumerable<TM7Threat>;
            if (threats?.Any() ?? false)
            {
                foreach (var threat in threats)
                {
                    if (threat != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        var risk = model.GetRisk(threat.title);
                        if (risk == null)
                        {
                            risk = model.AddRisk(threat.title);
                            if (!string.IsNullOrWhiteSpace(threat.description))
                            {
                                sb.AppendLine(threat.description);
                                sb.AppendLine();
                            }
                            sb.AppendLine("Applies to the following Data Flow(s):");
                        }
                        else
                        {
                            sb.Append(risk.Description);
                        }

                        sb.AppendLine($"- {threat.interaction}");

                        if (risk != null)
                        {
                            risk.Description = sb.ToString();
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}

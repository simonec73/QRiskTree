using QRiskTree.Engine.ImportExport.OpenThreatModel;
using System.Text;

namespace QRiskTree.Engine.ImportExport
{
    public class OpenTMImporter : IImporter
    {
        public string FileDescription => "Open Threat Model";

        public string FileExtension => ".json";

        public bool TryImportingIntoRiskModel<R, M>(string filePath, ref IRiskModel<R, M> model) where R : INamedObject where M : INamedObject
        {
            if (model == null) return false;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

            var openThreatModel = OpenThreatModelImporter.Import(filePath);

            if (openThreatModel != null)
            {
                var threats = openThreatModel.Threats?.ToArray();
                if (threats?.Any() ?? false)
                {
                    foreach (var threat in threats)
                    {
                        if (threat != null)
                        {
                            var risk = model.AddRisk(threat.Name);

                            var builder = new StringBuilder();
                            if (!string.IsNullOrWhiteSpace(threat.Description))
                            {
                                builder.AppendLine(threat.Description);
                            }

                            var categories = threat.Categories?.ToArray();
                            bool first = true;
                            if (categories?.Any() ?? false)
                            {
                                if (builder.Length > 0)
                                    builder.AppendLine();

                                builder.Append("Categories: ");
                                foreach (var category in categories)
                                {
                                    if (!first)
                                        builder.Append(", ");
                                    else
                                        first = false;
                                    builder.Append(category);
                                }
                                builder.AppendLine();
                            }

                            var cwes = threat.Cwes?.ToArray();
                            first = true;
                            if (cwes?.Any() ?? false)
                            {
                                if (builder.Length > 0)
                                    builder.AppendLine();

                                builder.Append("CWEs: ");
                                foreach (var cwe in cwes)
                                {
                                    if (!first)
                                        builder.Append(", ");
                                    else
                                        first = false;
                                    builder.Append(cwe);
                                }
                                builder.AppendLine();
                            }

                            if (threat.Risk != null)
                            {
                                if (builder.Length > 0)
                                    builder.AppendLine();

                                if (threat.Risk.Likelihood != null && threat.Risk.Likelihood > 0.0)
                                {
                                    builder.AppendLine($"Likelihood: {threat.Risk.Likelihood}%.");
                                    if (!string.IsNullOrWhiteSpace(threat.Risk.LikelihoodComment))
                                        builder.AppendLine(threat.Risk.LikelihoodComment);
                                }

                                if (builder.Length > 0)
                                    builder.AppendLine();

                                if (threat.Risk.Impact != 0.0)
                                {
                                    builder.AppendLine($"Impact: {threat.Risk.Impact}%.");
                                    builder.AppendLine(threat.Risk.ImpactComment);
                                }
                            }

                            risk.Description = builder.ToString();
                        }
                    }
                }

                var mitigations = openThreatModel.Mitigations?.ToArray();
                if (mitigations?.Any() ?? false)
                {
                    foreach (var mitigation in mitigations)
                    {
                        if (mitigation != null)
                        {
                            var mitigationVM = model.AddMitigation(mitigation.Name);
                            if (mitigationVM != null)
                            {
                                var builder = new StringBuilder();
                                if (!string.IsNullOrWhiteSpace(mitigation.Description))
                                {
                                    builder.AppendLine(mitigation.Description);
                                }
                                if (mitigation.RiskReduction > 0.0)
                                {
                                    if (builder.Length > 0)
                                        builder.AppendLine();

                                    builder.Append($"Risk Reduction: {mitigation.RiskReduction}%.");
                                }
                                mitigationVM.Description = builder.ToString();
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

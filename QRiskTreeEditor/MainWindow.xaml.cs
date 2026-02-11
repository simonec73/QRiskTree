using QRiskTree.Encryption;
using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;
using QRiskTree.Engine.Model;
using QRiskTreeEditor.Controls;
using QRiskTreeEditor.Importers;
using QRiskTreeEditor.SecondaryWindows;
using QRiskTreeEditor.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TMFileParser;
using TMFileParser.Models.output;

namespace QRiskTreeEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _captionBase;
        private string _fileName = string.Empty;
        private bool _encrypted;
        private QRiskTree.Engine.Range? _baseline;
        private double _outputHeight;
        private StringBuilder _markdown = new StringBuilder();
        private EncryptionManager _encryptionManager = new EncryptionManager();

        public static readonly RoutedCommand SaveCommand = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();
            _captionBase = Title;

            var riskModel = RiskModel.Create();
            SetDataContext(new RiskModelViewModel(riskModel));

            _risksContainer.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            _mitigationsContainer.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            _factsContainer.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            _factAnalyzersContainer.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            
            CommandBindings.Add(new CommandBinding(SaveCommand, SaveCommand_Executed));
        }

        private void SetDataContext(RiskModelViewModel model)
        {
            DataContext = model;
            _chartBaseline.SetModel(model, RelevantEvent.Baseline);
            _chartFirst.SetModel(model, RelevantEvent.FirstYear);
            _chartFollowing.SetModel(model, RelevantEvent.FollowingYears);
            _chartComparison.SetModel(model, RelevantEvent.BaselineAndOptimizationTarget);
            _tabFactAnalyzerResults.Visibility = Visibility.Collapsed;
            SubscribeMitigatedRisks();

            _output.Markdown = string.Empty;
            _markdown.Clear();

            _tabControl.SelectedIndex = 0;
        }

        #region Baseline management.
        private void SubscribeMitigatedRisks()
        {
            if (DataContext is RiskModelViewModel model)
            {
                model.Risks.CollectionChanged += MitigatedRisks_CollectionChanged;
                foreach (MitigatedRiskViewModel risk in model.Risks)
                {
                    risk.PropertyChanged += MitigatedRisk_PropertyChanged;
                }
            }
        }

        private void MitigatedRisks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Subscribe/unsubscribe PropertyChanged for added/removed items
            if (e.NewItems != null)
            {
                foreach (MitigatedRiskViewModel risk in e.NewItems)
                    risk.PropertyChanged += MitigatedRisk_PropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (MitigatedRiskViewModel risk in e.OldItems)
                    risk.PropertyChanged -= MitigatedRisk_PropertyChanged;
            }
            InvalidateBaseline();
        }

        private void MitigatedRisk_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MitigatedRiskViewModel.Min) ||
                  e.PropertyName == nameof(MitigatedRiskViewModel.Mode) ||
                  e.PropertyName == nameof(MitigatedRiskViewModel.Max) ||
                  e.PropertyName == nameof(MitigatedRiskViewModel.Confidence) ||
                  e.PropertyName == nameof(MitigatedRiskViewModel.Components) ||
                  e.PropertyName == nameof(MitigatedRiskViewModel.IsEnabled))
            {
                InvalidateBaseline();
            }
        }

        private void InvalidateBaseline()
        {
            _baseline = null;
        }
        #endregion

        #region Menu handlers.
        #region File menu handlers.
        private void _fileNew_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                if (MessageBox.Show("Are you sure you want to create a new model?\nUnsaved changes will be lost.", "Confirm New Model", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    modelVM.Model?.Dispose();
                    _fileName = string.Empty;
                    Title = _captionBase;
                    _encrypted = false;
                    SetDataContext(new RiskModelViewModel(RiskModel.Create()));
                    _encryptionManager.ResetPassphrase();
                }
            }
            else
            {
                _fileName = string.Empty;
                Title = _captionBase;
                _encrypted = false;
                SetDataContext(new RiskModelViewModel(RiskModel.Create()));
                _encryptionManager.ResetPassphrase();
            }
        }

        private void _fileOpen_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                if (MessageBox.Show("Are you sure you want to open a new model?\nUnsaved changes will be lost.", "Confirm Open Model", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    modelVM.Model?.Dispose();

                    var openFileDialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = "Open QRiskTree File",
                        Filter = "QRiskTree files (*.json)|*.json|QRiskTree encrypted files (*.qrisk)|*.qrisk|All files (*.*)|*.*",
                        DefaultExt = ".json",
                        CheckFileExists = true
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            _fileName = openFileDialog.FileName;
                            RiskModel? riskModel = null;
                            if (openFileDialog.FilterIndex == 2 || Path.GetExtension(_fileName).Equals(".qrisk", StringComparison.OrdinalIgnoreCase))
                            {
                                // Get the password
                                var dialog = new SinglePassword();
                                if (dialog.ShowDialog() == true)
                                {
                                    _encryptionManager.SetPassphrase(dialog.Password);
                                }

                                // Load the encrypted file.
                                var binaryProtocol = new BinaryProtocol<RiskModel>();
                                try
                                {
                                    using (var stream = File.OpenRead(_fileName))
                                    {
                                        riskModel = binaryProtocol.Read(stream, _encryptionManager);
                                        if (riskModel != null)
                                            _encrypted = true;
                                    }
                                }
                                catch (Exception exc)
                                {
                                    MessageBox.Show(exc.Message, "Load from encrypted file failed", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                // Regular file
                                riskModel = RiskModel.Load(_fileName);
                                _encryptionManager.ResetPassphrase();
                            }

                            if (riskModel != null)
                            {
                                riskModel.CompleteLoad();
                                SetDataContext(new RiskModelViewModel(riskModel));
                                Title = $"{_captionBase} - {Path.GetFileName(_fileName)}";

                                MessageBox.Show("File loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void _fileSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                _fileSaveAs_Click(sender, e);
                return;
            }
            else if (DataContext is RiskModelViewModel modelVM)
            {
                if (_encrypted)
                {
                    // Save encrypted
                    var binaryProtocol = new BinaryProtocol<RiskModel>();
                    try
                    {
                        using (var stream = File.Open(_fileName, FileMode.Create, FileAccess.Write))
                        {
                            binaryProtocol.Write(modelVM.Model, stream, _encryptionManager);
                        }
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.Message, "Save to encrypted file failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    modelVM.Model.Serialize(_fileName);
                }
                    
                MessageBox.Show("File saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            _fileSave_Click(sender, e);
        }

        private void _fileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save QRiskTree File",
                Filter = "QRiskTree files (*.json)|*.json|QRiskTree encrypted files (*.qrisk)|*.qrisk|All files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (DataContext is RiskModelViewModel modelVM)
                    {
                        _fileName = saveFileDialog.FileName;

                        if (saveFileDialog.FilterIndex == 2 || Path.GetExtension(_fileName).Equals(".qrisk", StringComparison.OrdinalIgnoreCase))
                        {
                            // Get the password
                            var dialog = new DoublePassword();
                            if (dialog.ShowDialog() == true)
                            {
                                _encrypted = true;
                                _encryptionManager.SetPassphrase(dialog.Password);

                                // Save encrypted
                                var binaryProtocol = new BinaryProtocol<RiskModel>();
                                try
                                {
                                    using (var stream = File.Open(_fileName, FileMode.Create, FileAccess.Write))
                                    {
                                        binaryProtocol.Write(modelVM.Model, stream, _encryptionManager);
                                    }
                                }
                                catch (Exception exc)
                                {
                                    MessageBox.Show(exc.Message, "Save to encrypted file failed", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            _encrypted = false;
                            _encryptionManager.ResetPassphrase();
                            modelVM.Model.Serialize(_fileName);
                        }

                        Title = $"{_captionBase} - {Path.GetFileName(_fileName)}";
                        MessageBox.Show("File saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void _fileAbout_Click(object sender, RoutedEventArgs e)
        {
            (new About()).ShowDialog();
        }

        private void _fileExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to exit?\nUnsaved changes will be lost.", "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
        #endregion

        #region Edit menu handlers.
        private void _editCreateRisk_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddRisk("New Risk");
            }
        }

        private void _editCreateMitigation_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddMitigation("New Mitigation");
            }
        }

        private void _editCreateFact_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddFact(new FactHardNumber("Context", "Name of the source", "New Fact", 0.0));
            }
        }

        private void _editCreateFactWithNumericRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddFact(new FactRange("Context", "Name of the source", "New Fact",
                    new QRiskTree.Engine.Range(QRiskTree.Engine.RangeType.Number, 0.0, 0.0, 0.0, QRiskTree.Engine.Confidence.Low)));
            }
        }

        private void _editCreateFactWithMonetaryRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddFact(new FactRange("Context", "Name of the source", "New Fact", 
                    new QRiskTree.Engine.Range(QRiskTree.Engine.RangeType.Money, 0.0, 0.0, 0.0, QRiskTree.Engine.Confidence.Low)));
            }
        }

        private void _editCreateFactWithFrequencyRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddFact(new FactRange("Context", "Name of the source", "New Fact",
                    new QRiskTree.Engine.Range(QRiskTree.Engine.RangeType.Frequency, 0.0, 0.0, 0.0, QRiskTree.Engine.Confidence.Low)));
            }
        }

        private void _editCreateFactWithPercentageRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddFact(new FactRange("Context", "Name of the source", "New Fact",
                    new QRiskTree.Engine.Range(QRiskTree.Engine.RangeType.Percentage, 0.0, 0.0, 0.0, QRiskTree.Engine.Confidence.Low)));
            }
        }

        private void _editCreateFactAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                modelVM.AddFactAnalyzer("New Fact Analyzer");
            }
        }

        private void _clearOutput_Click(object sender, RoutedEventArgs e)
        {
            _output.Markdown = string.Empty;
            _markdown.Clear();
        }
        #endregion

        #region View menu handlers.
        private void _viewToggleRiskProperties_Click(object sender, RoutedEventArgs e)
        {
            switch (_riskProperties.Visibility)
            {
                case Visibility.Visible:
                    _viewToggleRiskProperties.Header = "Show Risk Properties";
                    _riskProperties.Visibility = Visibility.Collapsed;
                    break;
                default:
                    _viewToggleRiskProperties.Header = "Hide Risk Properties";
                    _riskProperties.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void _viewToggleMitigationProperties_Click(object sender, RoutedEventArgs e)
        {
            switch (_mitigationProperties.Visibility)
            {
                case Visibility.Visible:
                    _viewToggleMitigationProperties.Header = "Show Mitigation Properties";
                    _mitigationProperties.Visibility = Visibility.Collapsed;
                    break;
                default:
                    _viewToggleMitigationProperties.Header = "Hide Mitigation Properties";
                    _mitigationProperties.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void _viewToggleFactsProperties_Click(object sender, RoutedEventArgs e)
        {
            switch (_factProperties.Visibility)
            {
                case Visibility.Visible:
                    _viewToggleFactsProperties.Header = "Show Fact Properties";
                    _factProperties.Visibility = Visibility.Collapsed;
                    break;
                default:
                    _viewToggleFactsProperties.Header = "Hide Fact Properties";
                    _factProperties.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void _viewToggleOutput_Click(object sender, RoutedEventArgs e)
        {
            var grid = (Grid)_tabControl.Parent;

            switch (_output.Visibility)
            {
                case Visibility.Visible:
                    _viewToggleOutput.Header = "Show Output";
                    _splitter.Visibility = Visibility.Collapsed;
                    _output.Visibility = Visibility.Collapsed;
                    _outputHeight = grid.RowDefinitions[3].Height.Value;
                    grid.RowDefinitions[2].Height = new GridLength(0);              // Splitter row
                    grid.RowDefinitions[3].Height = new GridLength(0);              // Output row
                    break;
                default:
                    _viewToggleOutput.Header = "Hide Output";
                    _splitter.Visibility = Visibility.Visible;
                    _output.Visibility = Visibility.Visible;
                    grid.RowDefinitions[2].Height = GridLength.Auto;                // Splitter row
                    grid.RowDefinitions[3].Height = new GridLength(_outputHeight);  // Output row
                    break;
            }
        }

        private void _viewHide_Click(object sender, RoutedEventArgs e)
        {
            _viewToggleRiskProperties.Header = "Show Risk Properties";
            _riskProperties.Visibility = Visibility.Collapsed;
            _viewToggleMitigationProperties.Header = "Show Mitigation Properties";
            _mitigationProperties.Visibility = Visibility.Collapsed;
            _viewToggleFactsProperties.Header = "Show Fact Properties";
            _factProperties.Visibility = Visibility.Collapsed;
            var grid = (Grid)_tabControl.Parent;
            _viewToggleOutput.Header = "Show Output";
            _splitter.Visibility = Visibility.Collapsed;
            _output.Visibility = Visibility.Collapsed;
            _outputHeight = grid.RowDefinitions[3].Height.Value;
            grid.RowDefinitions[2].Height = new GridLength(0);              // Splitter row
            grid.RowDefinitions[3].Height = new GridLength(0);              // Output row
        }

        private void _viewShow_Click(object sender, RoutedEventArgs e)
        {
            _viewToggleRiskProperties.Header = "Hide Risk Properties";
            _riskProperties.Visibility = Visibility.Visible;
            _viewToggleMitigationProperties.Header = "Hide Mitigation Properties";
            _mitigationProperties.Visibility = Visibility.Visible;
            _viewToggleFactsProperties.Header = "Hide Fact Properties";
            _factProperties.Visibility = Visibility.Visible;
            var grid = (Grid)_tabControl.Parent;
            _viewToggleOutput.Header = "Hide Output";
            _splitter.Visibility = Visibility.Visible;
            _output.Visibility = Visibility.Visible;
            grid.RowDefinitions[2].Height = GridLength.Auto;                // Splitter row
            if (_outputHeight > 0)
                grid.RowDefinitions[3].Height = new GridLength(_outputHeight);  // Output row
        }
        #endregion

        #region Import menu handlers.
        private void _importFacts_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Facts File",
                Filter = "Facts files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                if (DataContext is RiskModelViewModel modelVM)
                    modelVM.Model.ImportFacts(openFileDialog.FileName);
            }
        }

        private void _importFromTMT_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Microsoft Threat Modeling Tool Threat Model",
                Filter = "Threat Model files (*.tm7)|*.tm7|All files (*.*)|*.*",
                DefaultExt = ".tm7",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var reader = new TM7FileReader(new FileInfo(openFileDialog.FileName));
                var threats = (IEnumerable<object>)reader.GetData("threats") as IEnumerable<TM7Threat>;
                if (threats?.Any() ?? false)
                {
                    if (DataContext is RiskModelViewModel modelVM)
                    {
                        foreach (var threat in threats)
                        {
                            if (threat != null)
                            {
                                StringBuilder sb = new StringBuilder();
                                var risk = modelVM.Risks.OfType<MitigatedRiskViewModel>()
                                    .FirstOrDefault(x => string.CompareOrdinal(x.Name, threat.title) == 0);
                                if (risk == null)
                                {
                                    risk = modelVM.AddRisk(threat.title);
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
                    }
                }
            }
        }

        private void _importFromOpenTM_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Open Threat Model file",
                Filter = "Open Threat Model files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var model = OpenThreatModelImporter.Import(openFileDialog.FileName);

                if (model != null)
                {
                    if (DataContext is RiskModelViewModel modelVM)
                    {
                        var threats = model.Threats?.ToArray();
                        if (threats?.Any() ?? false)
                        {
                            foreach (var threat in threats)
                            {
                                if (threat != null)
                                {
                                    var risk = modelVM.AddRisk(threat.Name);

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

                        var mitigations = model.Mitigations?.ToArray();
                        if (mitigations?.Any() ?? false)
                        {
                            foreach (var mitigation in mitigations)
                            {
                                if (mitigation != null)
                                {
                                    var mitigationVM = modelVM.AddMitigation(mitigation.Name);
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
                    }
                }
            }
        }
        #endregion

        #region Export menu handlers.

        private void _exportFacts_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Facts",
                Filter = "Facts files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (DataContext is RiskModelViewModel modelVM)
                        modelVM.Model.ExportFacts(saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void _exportOutput_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Output",
                Filter = "Markdown files (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".md"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, _output.Markdown);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region Calculation menu handlers.
        private void _calculateBaseline_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                var risks = modelVM.Risks?.OfType<MitigatedRiskViewModel>()?.Where(x => x.IsEnabled).ToArray();
                if (risks?.Any() ?? false)
                {
                    string? invalidRiskName = null;
                    Node? violatingNode = null;
                    foreach (var risk in risks)
                    {
                        if (!risk.Node.CanBeSimulated(out var node))
                        {
                            violatingNode = node;
                            invalidRiskName = risk.Name;
                            break;
                        }
                    }

                    if (invalidRiskName == null)
                    {
                        AppendText("# Calculating Baseline Risk");
                        AppendText($"**Model:** {modelVM.Properties.Name}");
                        AppendText();
                        AppendText($"**Created on:** {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}");
                        AppendText();
                        AppendText("## Baseline Definition");
                        foreach (var risk in risks)
                        {
                            AppendText($"- Risk: {risk.Name}");
                        }

                        uint iterations = modelVM.Properties.Iterations;

                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        try
                        {
                            Mouse.OverrideCursor = Cursors.Wait;
                            _baseline = modelVM.Model.Simulate(iterations);
                        }
                        finally
                        {
                            Mouse.OverrideCursor = null;
                            stopwatch.Stop();
                        }

                        AppendText("## Baseline Risk Results");

                        var currencySymbol = modelVM.Properties.CurrencySymbol;
                        var monetaryScale = modelVM.Properties.MonetaryScale;

                        if (_baseline != null)
                        {
                            AppendText($"- {modelVM.Properties.MinPercentile}th percentile: {_baseline.GetMin(currencySymbol, monetaryScale)}");
                            AppendText($"- Mode: {_baseline.GetMode(currencySymbol, monetaryScale)}");
                            AppendText($"- {modelVM.Properties.MaxPercentile}th percentile: {_baseline.GetMax(currencySymbol, monetaryScale)}");
                            AppendText($"- Confidence: {_baseline.Confidence}");
                        }
                        AppendText();
                        AppendText($"Risk for the baseline calculated in {stopwatch.ElapsedMilliseconds}ms.");
                        AppendText();
                        AppendText();
                    }
                    else
                    {
                        if (violatingNode != null && !(violatingNode is MitigatedRisk))
                        {
                            MessageBox.Show($"Risk '{invalidRiskName}' is not valid for baseline factAnalyzer calculation because {violatingNode.GetType().Name.AddSpacesToCamelCase()} '{violatingNode.Name}' is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show($"Risk '{invalidRiskName}' is not valid for baseline factAnalyzer calculation.\nPlease check if it has all required children, or if you still must set its range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No factAnalyzer has been selected for baseline factAnalyzer calculation.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private async void _calculateOptimalMitigations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _calculateOptimalMitigations_ClickAsync(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during optimization: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task _calculateOptimalMitigations_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                var risks = modelVM.Risks?.OfType<MitigatedRiskViewModel>()?.Where(x => x.IsEnabled).ToArray();
                var mitigations = modelVM.Mitigations?.OfType<MitigationCostViewModel>()?.Where(x => x.IsEnabled).ToArray();
                if ((risks?.Any() ?? false) && (mitigations?.Any() ?? false))
                {
                    string? invalidRiskName = null;
                    Node? violatingNode = null;
                    foreach (var risk in risks)
                    {
                        if (!risk.Node.CanBeSimulated(out var node))
                        {
                            violatingNode = node;
                            invalidRiskName = risk.Name;
                            break;
                        }
                    }

                    if (invalidRiskName == null)
                    {
                        int parallelization = RiskModel.OptimalParallelism;
                        int totalCombinations = (1 << risks.Length) - 1;
                        int countIterations = (int)Math.Ceiling(((decimal)totalCombinations) / parallelization);
                        int countAverageTreeSize = (int)Math.Ceiling(((decimal)RecursiveCount(risks)) / risks.Length);
                        int countAverageAppliedMitigations = (int)Math.Ceiling(((decimal)risks.Sum(x => (x.Node.Children?.OfType<AppliedMitigation>()?.Count() ?? 0))) / risks.Length);
                        int countMitigations = mitigations.Length;
                        int estimatedRequiredTime = (int) Math.Ceiling((decimal)
                            (countIterations * (countAverageTreeSize + countAverageAppliedMitigations) + countMitigations * 2) 
                            * 10 * ((decimal)modelVM.Properties.Iterations) / ((decimal)Node.DefaultIterations));
                        bool proceed;
                        if (estimatedRequiredTime > 60000)
                        {
                            proceed = MessageBox.Show($"The calculation of the optimal mitigations might require about {(estimatedRequiredTime / 60000).ToString("N0")} minutes. Do you want to proceed?",
                                "Long running calculation", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
                        }
                        else
                        {
                            proceed = true;
                        }

                        if (proceed)
                        {
                            AppendText("# Optimal Mitigations Set Calculation");
                            AppendText($"**Model:** {modelVM.Properties.Name}");
                            AppendText();
                            AppendText($"**Created on:** {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}");
                            AppendText();
                            AppendText("## Baseline Definition");

#if DEBUG
                            modelVM.Model.FirstYearSimulationCompleted += Model_FirstYearSimulationCompleted;
#endif

                            AppendText("## Population Definition");
                            AppendText("### Included Risks");
                            foreach (var risk in risks)
                            {
                                AppendText($"- Risk: {risk.Name}");
                            }

                            AppendText("### Included Mitigations");
                            foreach (var mitigation in mitigations)
                            {
                                AppendText($"- Mitigation: {mitigation.Name}");
                            }

                            uint iterations = modelVM.Properties.Iterations;
                            var optParameter = modelVM.Properties.OptimizationParameter;
                            var ignoreImplementationCosts = modelVM.Properties.IgnoreImplementationCosts;
                            var notText = ignoreImplementationCosts ? "not " : "";
                            AppendText($"Optimization has been calculated on the {optParameter} parameter, and has {notText}considered the Implementation costs.");

                            var currencySymbol = modelVM.Properties.CurrencySymbol;
                            var monetaryScale = modelVM.Properties.MonetaryScale;

                            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                            IEnumerable<MitigationCost>? optimized = null;
                            QRiskTree.Engine.Range? firstYearCosts = null;
                            QRiskTree.Engine.Range? followingYearsCosts = null;

                            try
                            {
                                Mouse.OverrideCursor = Cursors.Wait;
                                var simulationResult = await modelVM.Model.OptimizeMitigationsAsync(optParameter, ignoreImplementationCosts, iterations);
                                if (simulationResult != null)
                                {
                                    firstYearCosts = simulationResult.FirstYear;
                                    followingYearsCosts = simulationResult.FollowingYears;
                                    optimized = modelVM.Model.Mitigations
                                        .Where(x => simulationResult.SelectedMitigations?.Contains(x.Id) ?? false)
                                        .ToArray();
                                }
                            }
                            finally
                            {
                                Mouse.OverrideCursor = null;
                                stopwatch.Stop();

#if DEBUG
                                modelVM.Model.FirstYearSimulationCompleted -= Model_FirstYearSimulationCompleted;
#endif
                            }

                            AppendText("## Optimization Results");

                            var builder = new StringBuilder();
                            var negativeDelta = false;
                            if (firstYearCosts != null)
                            {
                                var format = firstYearCosts.GetFormat(currencySymbol, monetaryScale);

                                AppendText("### Estimation of the Minimal Overall Yearly Cost for the first year");
                                builder.Append($"- {modelVM.Properties.MinPercentile}th percentile: {firstYearCosts.GetMin(currencySymbol, monetaryScale)}");
                                if (_baseline != null)
                                {
                                    builder.Append($" (saving {(_baseline.Min - firstYearCosts.Min).ToString(format)}, equal to {((_baseline.Min - firstYearCosts.Min) / _baseline.Min).ToString("P2")})");
                                    if (_baseline.Min - firstYearCosts.Min < 0)
                                        negativeDelta = true;
                                }
                                AppendText(builder.ToString());
                                builder.Clear();
                                builder.Append($"- Mode: {firstYearCosts.GetMode(currencySymbol, monetaryScale)}");
                                if (_baseline != null)
                                {
                                    builder.Append($" (saving {(_baseline.Mode - firstYearCosts.Mode).ToString(format)}, equal to {((_baseline.Mode - firstYearCosts.Mode) / _baseline.Mode).ToString("P2")})");
                                    if (_baseline.Mode - firstYearCosts.Mode < 0)
                                        negativeDelta = true;
                                }
                                AppendText(builder.ToString());
                                builder.Clear();
                                builder.Append($"- {modelVM.Properties.MaxPercentile}th percentile: {firstYearCosts.GetMax(currencySymbol, monetaryScale)}");
                                if (_baseline != null)
                                {
                                    builder.Append($" (saving {(_baseline.Max - firstYearCosts.Max).ToString(format)}, equal to {((_baseline.Max - firstYearCosts.Max) / _baseline.Max).ToString("P2")})");
                                    if (_baseline.Max - firstYearCosts.Max < 0)
                                        negativeDelta = true;
                                }
                                AppendText(builder.ToString());
                                AppendText($"- Confidence: {firstYearCosts.Confidence}\n");
                            }

                            if (followingYearsCosts != null)
                            {
                                var format = followingYearsCosts.GetFormat(currencySymbol, monetaryScale);

                                AppendText("### Estimation of the Minimal Overall Yearly Cost for the following years");
                                builder.Clear();
                                builder.Append($"- {modelVM.Properties.MinPercentile}th percentile: {followingYearsCosts.GetMin(currencySymbol, monetaryScale)}");
                                if (_baseline != null)
                                {
                                    builder.Append($" (saving {(_baseline.Min - followingYearsCosts.Min).ToString(format)}, equal to {((_baseline.Min - followingYearsCosts.Min) / _baseline.Min).ToString("P2")})");
                                    if (_baseline.Min - followingYearsCosts.Min < 0)
                                        negativeDelta = true;
                                }
                                AppendText(builder.ToString());
                                builder.Clear();
                                builder.Append($"- Mode: {followingYearsCosts.GetMode(currencySymbol, monetaryScale)}");
                                if (_baseline != null)
                                {
                                    builder.Append($" (saving {(_baseline.Mode - followingYearsCosts.Mode).ToString(format)}, equal to {((_baseline.Mode - followingYearsCosts.Mode) / _baseline.Mode).ToString("P2")})");
                                    if (_baseline.Mode - followingYearsCosts.Mode < 0)
                                        negativeDelta = true;
                                }
                                AppendText(builder.ToString());
                                builder.Clear();
                                builder.Append($"- {modelVM.Properties.MaxPercentile}th percentile: {followingYearsCosts.GetMax(currencySymbol, monetaryScale)}");
                                if (_baseline != null)
                                {
                                    builder.Append($" (saving {(_baseline.Max - followingYearsCosts.Max).ToString(format)}, equal to {((_baseline.Max - followingYearsCosts.Max) / _baseline.Max).ToString("P2")})");
                                    if (_baseline.Max - followingYearsCosts.Max < 0)
                                        negativeDelta = true;
                                }
                                AppendText(builder.ToString());
                                AppendText($"- Confidence: {followingYearsCosts.Confidence}\n");
                            }

                            if (optimized?.Any() ?? false)
                            {
                                if (negativeDelta)
                                {
                                    AppendText("**Warning:** Some costs savings are negative, meaning that the selected mitigations increase the overall factAnalyzer cost compared to the baseline. Please review the mitigations' costs and factAnalyzer reductions. If everything is fine, please repeat the Optimization. If this happens again, it might be the case that the identified mitigations do not improve the situation and should be avoided.");
                                }
                                else
                                {
                                    AppendText("### Optimal mitigations");
                                    foreach (var mitigation in optimized)
                                    {
                                        AppendText($"- {mitigation.Name}");
                                        AppendText($"  - Implementation Costs: {mitigation.GetMin(currencySymbol, monetaryScale)} - {mitigation.GetMode(currencySymbol, monetaryScale)} - {mitigation.GetMax(currencySymbol, monetaryScale)} ({mitigation.Confidence})");
                                        if (mitigation.OperationCosts != null)
                                        {
                                            AppendText($"  - Operation Costs: {mitigation.OperationCosts.GetMin(currencySymbol, monetaryScale)} - {mitigation.OperationCosts.GetMode(currencySymbol, monetaryScale)} - {mitigation.OperationCosts.GetMax(currencySymbol, monetaryScale)} ({mitigation.OperationCosts.Confidence})");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                AppendText("The selected mitigations increase the overall factAnalyzer cost compared to the baseline. Please review the mitigations' costs and factAnalyzer reductions. If everything is fine, please repeat the Optimization. If this happens again, it might be the case that the identified mitigations do not improve the situation and should be avoided.");
                            }
                            AppendText();
                            AppendText($"Optimization completed in {stopwatch.ElapsedMilliseconds}ms.");
                            AppendText();
                        }
                    }
                    else
                    {
                        if (violatingNode != null && !(violatingNode is MitigatedRisk))
                        {
                            MessageBox.Show($"Risk '{invalidRiskName}' is not valid for optimal mitigations calculation because {violatingNode.GetType().Name.AddSpacesToCamelCase()} '{violatingNode.Name}' is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show($"Risk '{invalidRiskName}' is not valid for optimal mitigations calculation.\nPlease check if it has all required children, or if you still must set its range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No factAnalyzers or mitigations selected for optimized set calculation.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

#if DEBUG
        private void Model_FirstYearSimulationCompleted(IEnumerable<Guid>? selectedMitigations, double[] samples)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                var currencySymbol = modelVM.Properties.CurrencySymbol;
                var monetaryScale = modelVM.Properties.MonetaryScale;

                var range = samples.ToRange(RangeType.Money, 
                    modelVM.Properties.MinPercentile, modelVM.Properties.MaxPercentile);
                AppendText($"Simulation with {selectedMitigations?.Count() ?? 0} mitigations - Min: {range?.GetMin(currencySymbol, monetaryScale)} - Mode: {range?.GetMode(currencySymbol, monetaryScale)} - Max: {range?.GetMax(currencySymbol, monetaryScale)} ({range?.Confidence}).");
            }
        }
#endif

        private int RecursiveCount(IEnumerable<NodeViewModel> nodes)
        {
            int count = 0;

            if (nodes?.Any() ?? false)
            {
                foreach (var node in nodes)
                {
                    var children = node.Components?.OfType<NodeViewModel>()?.ToArray();
                    var mitigations = (node as MitigatedRiskViewModel)?.Mitigations?.OfType<AppliedMitigationViewModel>()?.ToArray();
                    if (node.IsSetByUser || !(children?.Any() ?? false))
                        count++;
                    else if (children?.Any() ?? false)
                    {
                        count += RecursiveCount(children);
                    }
                    count += mitigations?.Count() ?? 0;
                }
            }

            return count;
        }

        private void _calculateAllFactAnalyzers_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                var factAnalyzers = modelVM.FactAnalyzers?.OfType<FactAnalyzerViewModel>()?.ToArray();
                if (factAnalyzers?.Any() ?? false)
                {
                    string? invalidFactAnalyzerName = null;
                    Node? violatingNode = null;
                    foreach (var factAnalyzer in factAnalyzers)
                    {
                        if (!factAnalyzer.Node.CanBeSimulated(out var node))
                        {
                            violatingNode = node;
                            invalidFactAnalyzerName = factAnalyzer.Name;
                            break;
                        }
                    }

                    if (invalidFactAnalyzerName == null)
                    {
                        AppendText("# Calculating Fact Analyzers");
                        AppendText($"**Model:** {modelVM.Properties.Name}");
                        AppendText();
                        AppendText($"**Created on:** {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}");
                        AppendText();
                        foreach (var factAnalyzer in factAnalyzers)
                        {
                            if (factAnalyzer.Node.Simulate(modelVM.Properties.MinPercentile,
                                modelVM.Properties.MaxPercentile, modelVM.Properties.Iterations))
                            {
                                AppendText($"## Fact Analyzer '{factAnalyzer.Name}'");
                                AppendText($"- Operation: {factAnalyzer.Operation.ToString()}");
                                AppendText($"- {modelVM.Properties.MinPercentile}th percentile: {factAnalyzer.Min.ToString("F2")}");
                                AppendText($"- Mode: {factAnalyzer.Mode.ToString("F2")}");
                                AppendText($"- {modelVM.Properties.MaxPercentile}th percentile: {factAnalyzer.Max.ToString("F2")}");
                                AppendText($"- Confidence: {factAnalyzer.Confidence}");
                            }
                        }
                        AppendText();
                        AppendText();
                    }
                    else
                    {
                        if (violatingNode != null)
                        {
                            MessageBox.Show($"Fact Analyzer '{invalidFactAnalyzerName}' is not valid because {violatingNode.GetType().Name.AddSpacesToCamelCase()} '{violatingNode.Name}' is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show($"Fact Analyzer '{invalidFactAnalyzerName}' is not valid.\nPlease check if it has all required children, or if you still must set its range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No Fact Analyzer has been defined.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        #endregion
        #endregion

        #region Other event handlers.
        private void ToggleRowDetails(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is object item)
            {
                // Try to find the DataGridRow in any DataGrid
                DependencyObject parent = button;
                while (parent != null && parent is not DataGrid)
                    parent = VisualTreeHelper.GetParent(parent);

                if (parent is DataGrid grid)
                {
                    var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                    if (row != null)
                    {
                        row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible
                            ? Visibility.Collapsed
                            : Visibility.Visible;

                        if (row.DetailsVisibility == Visibility.Visible)
                        {
                            button.Content = "-";
                        }
                        else
                        {
                            button.Content = "+";
                        }
                    }
                }
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
        #endregion

        #region Context menu management.
        private void OpeningContextMenu(object sender, ContextMenuEventArgs e)
        {
            var result = false;

            var row = GetDataGridRow(e.OriginalSource as DependencyObject);
            if (row != null)
            {
                result = OpenContextMenuForRow(row);
            }
            else
            {
                var grid = GetRootDataGrid(e.OriginalSource as DependencyObject);
                if (grid != null)
                {
                    // If the context menu is opened on the grid itself, we can open a context menu for the grid.
                    result = OpenContextMenuForGrid(grid);
                }
            }

            e.Handled = true;
        }

        private bool OpenContextMenuForRow(DataGridRow row)
        {
            var result = false;

            var modelVM = DataContext as RiskModelViewModel;

            ContextMenu? contextMenu = row.ContextMenu;
            if (contextMenu != null)
            {
                ClearContextMenu(contextMenu);
            }

            contextMenu = new ContextMenu();
            MenuItem item;
            if (row.DataContext is LossEventFrequencyViewModel lefVM)
            {
                if (lefVM.Node is LossEventFrequency lef)
                {
                    if (!(lef.Children?.OfType<ThreatEventFrequency>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Threat Event Frequency" };
                        item.Click += Item_AddThreatEventFrequency;
                        item.Tag = lefVM;
                        contextMenu.Items.Add(item);
                    }

                    if (!(lef.Children?.OfType<Vulnerability>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Vulnerability" };
                        item.Click += Item_AddVulnerability;
                        item.Tag = lefVM;
                        contextMenu.Items.Add(item);
                    }
                }
            }
            else if (row.DataContext is LossMagnitudeViewModel lmVM)
            {
                item = new MenuItem { Header = "Add Primary Loss" };
                item.Click += Item_AddPrimaryLoss;
                item.Tag = lmVM;
                contextMenu.Items.Add(item);

                item = new MenuItem { Header = "Add Secondary Risk" };
                item.Click += Item_AddSecondaryRisk;
                item.Tag = lmVM;
                contextMenu.Items.Add(item);
            }
            else if (row.DataContext is MitigatedRiskViewModel mrVM)
            {
                bool added = false;
                if (mrVM.Node is MitigatedRisk mr)
                {
                    if (!(mr.Children?.OfType<LossEventFrequency>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Loss Event Frequency" };
                        item.Click += Item_AddLossEventFrequency;
                        item.Tag = mrVM;
                        contextMenu.Items.Add(item);
                        added = true;
                    }

                    if (!(mr.Children?.OfType<LossMagnitude>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Loss Magnitude" };
                        item.Click += Item_AddLossMagnitude;
                        item.Tag = mrVM;
                        contextMenu.Items.Add(item);
                        added = true;
                    }
                }

                if (modelVM != null)
                {
                    var totalMitigations = modelVM.Mitigations?.OfType<MitigationCostViewModel>()?.Count() ?? 0;
                    var appliedMitigations = mrVM.Mitigations?.OfType<AppliedMitigationViewModel>()?.Count() ?? 0;
                    if (totalMitigations > appliedMitigations)
                    {
                        if (added)
                        {
                            contextMenu.Items.Add(new Separator());
                        }

                        item = new MenuItem { Header = "Associate Mitigation" };
                        item.Click += Item_AssociateMitigation;
                        item.Tag = mrVM;
                        contextMenu.Items.Add(item);
                    }
                }
            }
            else if (row.DataContext is SecondaryRiskViewModel srVM)
            {
                if (srVM.Node is SecondaryRisk sr)
                {
                    if (!(sr.Children?.OfType<SecondaryLossEventFrequency>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Secondary Loss Event Frequency" };
                        item.Click += Item_AddSecondaryLossEventFrequency;
                        item.Tag = srVM;
                        contextMenu.Items.Add(item);
                    }

                    if (!(sr.Children?.OfType<SecondaryLossMagnitude>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Secondary Loss Magnitude" };
                        item.Click += Item_AddSecondaryLossMagnitude;
                        item.Tag = srVM;
                        contextMenu.Items.Add(item);
                    }
                }
            }
            else if (row.DataContext is ThreatEventFrequencyViewModel tefVM)
            {
                if (tefVM.Node is ThreatEventFrequency tef)
                {
                    if (!(tef.Children?.OfType<ContactFrequency>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Contact Frequency" };
                        item.Click += Item_AddContactFrequency;
                        item.Tag = tefVM;
                        contextMenu.Items.Add(item);
                    }

                    if (!(tef.Children?.OfType<ProbabilityOfAction>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Probability of Action" };
                        item.Click += Item_AddProbabilityOfAction;
                        item.Tag = tefVM;
                        contextMenu.Items.Add(item);
                    }
                }
            }
            else if (row.DataContext is VulnerabilityViewModel vVM)
            {
                if (vVM.Node is Vulnerability v)
                {
                    if (!(v.Children?.OfType<ThreatCapability>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Threat Capability" };
                        item.Click += Item_AddThreatCapability;
                        item.Tag = vVM;
                        contextMenu.Items.Add(item);
                    }

                    if (!(v.Children?.OfType<ResistenceStrength>().Any() ?? false))
                    {
                        item = new MenuItem { Header = "Add Resistence Strength" };
                        item.Click += Item_AddResistenceStrength;
                        item.Tag = vVM;
                        contextMenu.Items.Add(item);
                    }
                }
            }

            if (contextMenu.Items.Count > 0)
            {
                contextMenu.Items.Add(new Separator());
            }

            if (row.DataContext is NodeViewModel nodeVM && modelVM != null)
            {
                var totalFacts = modelVM.Model.AvailableFacts?.Count() ?? 0;
                var associatedFacts = nodeVM.Facts?.OfType<LinkedFactViewModel>().Count() ?? 0;
                if (totalFacts > associatedFacts)
                {
                    item = new MenuItem { Header = "Associate Fact" };
                    item.Click += Item_AssociateFact;
                    item.Tag = nodeVM;
                    contextMenu.Items.Add(item);
                    contextMenu.Items.Add(new Separator());
                }

                var reset = false;
                if (nodeVM.IsSetByUser)
                {
                    item = new MenuItem { Header = "Reset the Range" };
                    item.Click += Item_ResetRange;
                    item.Tag = nodeVM;
                    contextMenu.Items.Add(item);
                    reset = true;
                }
                if (nodeVM is MitigationCostViewModel mcVM && mcVM.IsOperationCostSetByUser)
                {
                    item = new MenuItem { Header = "Reset the Operation Costs Range" };
                    item.Click += Item_ResetOperationCostsRange;
                    item.Tag = nodeVM;
                    contextMenu.Items.Add(item);
                    reset = true;
                }
                if (reset)
                {
                    contextMenu.Items.Add(new Separator());
                }
            }

            if (row.DataContext is MitigatedRiskViewModel riskVM)
            {
                item = new MenuItem { Header = "Clone the Mitigated Risk" };
                item.Click += Item_CloneRisk;
                item.Tag = riskVM;
                contextMenu.Items.Add(item);
                contextMenu.Items.Add(new Separator());
            }
            else if (row.DataContext is FactViewModel factVM)
            {
                item = new MenuItem { Header = "Clone the Fact" };
                item.Click += Item_CloneFact;
                item.Tag = factVM;
                contextMenu.Items.Add(item);
                contextMenu.Items.Add(new Separator());
            }
            else if (row.DataContext is FactAnalyzerViewModel factAnalyzerVM)
            {
                if (!(factAnalyzerVM.Parent is FactAnalyzerViewModel parent && parent.Parent is FactAnalyzerViewModel))
                {
                    item = new MenuItem { Header = "Add child Fact Analyzer" };
                    item.Click += Item_AddChildFactAnalyzer;
                    item.Tag = factAnalyzerVM;
                    contextMenu.Items.Add(item);
                }
                item = new MenuItem { Header = "Calculate the Fact Analyzer" };
                item.Click += Item_CalculateFactAnalyzer;
                item.Tag = factAnalyzerVM;
                contextMenu.Items.Add(item);
                item = new MenuItem { Header = "Clone the Fact Analyzer" };
                item.Click += Item_CloneFactAnalyzer;
                item.Tag = factAnalyzerVM;
                contextMenu.Items.Add(item);
                contextMenu.Items.Add(new Separator());
            }

            // Delete current row.
            item = new MenuItem { Header = $"Delete current {row.DataContext.GetType().Name.Replace("ViewModel", "").AddSpacesToCamelCase()}" };
            item.Click += Item_Delete;
            item.Tag = row.DataContext;
            contextMenu.Items.Add(item);

            row.ContextMenu = contextMenu;

            // Open the menu manually
            contextMenu.PlacementTarget = row;
            contextMenu.IsOpen = true;

            return result;
        }

        private bool OpenContextMenuForGrid(DataGrid grid)
        {
            var result = false;

            ContextMenu? contextMenu = grid.ContextMenu;
            if (contextMenu != null)
            {
                ClearContextMenu(contextMenu);
            }

            contextMenu = new ContextMenu();
            MenuItem item;
            switch (grid.Name)
            {
                case "_risks":
                    item = new MenuItem { Header = "Create a new Risk" };
                    item.Click += Item_CreateRisk;
                    contextMenu.Items.Add(item);
                    result = true;
                    break;
                case "_mitigations":
                    item = new MenuItem { Header = "Create a new Mitigation" };
                    item.Click += Item_CreateMitigation;
                    contextMenu.Items.Add(item);
                    result = true;
                    break;
                case "_facts":
                    item = new MenuItem { Header = "Create a simple Fact" };
                    item.Click += Item_CreateFact;
                    contextMenu.Items.Add(item);
                    item = new MenuItem { Header = "Create a Fact based on a numeric range" };
                    item.Click += Item_CreateFactWithNumericRange;
                    contextMenu.Items.Add(item);
                    item = new MenuItem { Header = "Create a Fact based on a monetary range" };
                    item.Click += Item_CreateFactWithMonetaryRange;
                    contextMenu.Items.Add(item);
                    item = new MenuItem { Header = "Create a Fact based on a frequency range" };
                    item.Click += Item_CreateFactWithFrequencyRange;
                    contextMenu.Items.Add(item);
                    item = new MenuItem { Header = "Create a Fact based on a percentage range" };
                    item.Click += Item_CreateFactWithPercentageRange;
                    contextMenu.Items.Add(item);
                    result = true;
                    break;
                case "_factAnalyzers":
                    item = new MenuItem { Header = "Create a new Fact Analyzer" };
                    item.Click += Item_CreateFactAnalyzer;
                    contextMenu.Items.Add(item);
                    result = true;
                    break;
            }

            if (result)
            {
                grid.ContextMenu = contextMenu;

                // Open the menu manually
                contextMenu.PlacementTarget = grid;
                contextMenu.IsOpen = true;
            }

            return result;
        }

        private void ClearContextMenu(ContextMenu contextMenu)
        {
            // Clear the existing context menu items.
            var menuItems = contextMenu.Items;
            foreach (var current in menuItems)
            {
                if (current is MenuItem menuItem)
                {
                    menuItem.Click -= Item_Delete;
                    menuItem.Click -= Item_AssociateFact;
                    menuItem.Click -= Item_AddThreatEventFrequency;
                    menuItem.Click -= Item_AddVulnerability;
                    menuItem.Click -= Item_AddPrimaryLoss;
                    menuItem.Click -= Item_AddSecondaryRisk;
                    menuItem.Click -= Item_AddLossEventFrequency;
                    menuItem.Click -= Item_AddLossMagnitude;
                    menuItem.Click -= Item_AssociateMitigation;
                    menuItem.Click -= Item_AddSecondaryLossEventFrequency;
                    menuItem.Click -= Item_AddSecondaryLossMagnitude;
                    menuItem.Click -= Item_AddContactFrequency;
                    menuItem.Click -= Item_AddProbabilityOfAction;
                    menuItem.Click -= Item_AddThreatCapability;
                    menuItem.Click -= Item_AddResistenceStrength;
                    menuItem.Click -= Item_ResetRange;
                    menuItem.Click -= Item_ResetOperationCostsRange;
                    menuItem.Click -= Item_CloneRisk;
                    menuItem.Click -= Item_CloneFact;
                    menuItem.Click -= Item_CloneFactAnalyzer;
                    menuItem.Click -= Item_CreateRisk;
                    menuItem.Click -= Item_CreateMitigation;
                    menuItem.Click -= Item_CreateFact;
                    menuItem.Click -= Item_CreateFactWithNumericRange;
                    menuItem.Click -= Item_CreateFactWithMonetaryRange;
                    menuItem.Click -= Item_CreateFactWithFrequencyRange;
                    menuItem.Click -= Item_CreateFactWithPercentageRange;
                    menuItem.Click -= Item_CreateFactAnalyzer;
                    menuItem.Click -= Item_AddChildFactAnalyzer;
                    menuItem.Click -= Item_CalculateFactAnalyzer;
                }
            }
        }

        #region Actions on the grid rows.
        private void Item_CloneRisk(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MitigatedRiskViewModel riskVM)
            {
                riskVM?.Clone();
            }
        }

        private void Item_CloneFact(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is FactViewModel factVM)
            {
                factVM?.Clone();
            }
        }

        private void Item_CloneFactAnalyzer(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is FactAnalyzerViewModel factAnalyzerVM)
            {
                factAnalyzerVM?.Clone();
            }
        }

        private void Item_ResetOperationCostsRange(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MitigationCostViewModel mcVM)
            {
                mcVM.ResetOperationCosts();
            }
        }

        private void Item_ResetRange(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is NodeViewModel nodeVM)
            {
                nodeVM.Reset();
            }
        }

        private void Item_AddResistenceStrength(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is VulnerabilityViewModel vVM)
            {
                vVM.AddResistenceStrength("New Resistence Strength");
            }
        }

        private void Item_AddThreatCapability(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is VulnerabilityViewModel vVM)
            {
                vVM.AddThreatCapability("New Threat Capability");
            }
        }

        private void Item_AddProbabilityOfAction(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is ThreatEventFrequencyViewModel tefVM)
            {
                tefVM.AddProbabilityOfAction("New Probability of Action");
            }
        }

        private void Item_AddContactFrequency(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is ThreatEventFrequencyViewModel tefVM)
            {
                tefVM.AddContactFrequency("New Contact Frequency");
            }
        }

        private void Item_AddSecondaryLossMagnitude(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is SecondaryRiskViewModel srVM)
            {
                srVM.AddSecondaryLossMagnitude("New Secondary Loss Magnitude");
            }
        }

        private void Item_AddSecondaryLossEventFrequency(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is SecondaryRiskViewModel srVM)
            {
                srVM.AddSecondaryLossEventFrequency("New Secondary Loss Event Frequency");
            }
        }

        private void Item_AssociateMitigation(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MitigatedRiskViewModel mrVM)
            {
                var alreadyApplied = mrVM.Mitigations?.OfType<AppliedMitigationViewModel>()?.ToArray();
                if (DataContext is RiskModelViewModel modelVM)
                {
                    var mitigations = modelVM.Mitigations?.OfType<MitigationCostViewModel>()?.ToArray();
                    if (mitigations?.Any() ?? false)
                    {
                        var notAppliedMitigations = mitigations
                            .Where(x => !alreadyApplied?.Any(y => y.MitigationCostId == x.Id) ?? true)
                            .ToArray();

                        if (notAppliedMitigations.Any())
                        {
                            var dialog = new AssociateMitigation(mrVM, notAppliedMitigations);
                            if (dialog.ShowDialog() ?? false)
                            {
                                var selectedMitigation = dialog.SelectedMitigation;
                                if (selectedMitigation != null)
                                {
                                    mrVM.ApplyMitigation(selectedMitigation, out var appliedMitigation);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Item_AddLossMagnitude(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MitigatedRiskViewModel mrVM)
            {
                mrVM.AddLossMagnitude("New Loss Magnitude");
            }
        }

        private void Item_AddLossEventFrequency(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MitigatedRiskViewModel mrVM)
            {
                mrVM.AddLossEventFrequency("New Loss Event Frequency");
            }
        }

        private void Item_AddSecondaryRisk(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is LossMagnitudeViewModel lmVM)
            {
                lmVM.AddSecondaryRisk("New Secondary Risk");
            }
        }

        private void Item_AddPrimaryLoss(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is LossMagnitudeViewModel lmVM)
            {
                lmVM.AddPrimaryLoss("New Primary Loss");
            }
        }

        private void Item_AddVulnerability(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is LossEventFrequencyViewModel lefVM)
            {
                lefVM.AddVulnerability("New Vulnerability");
            }
        }

        private void Item_AddThreatEventFrequency(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is LossEventFrequencyViewModel lefVM)
            {
                lefVM.AddThreatEventFrequency("New Threat Event Frequency");
            }
        }

        private void Item_AssociateFact(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is NodeViewModel nodeVM)
            {
                if (DataContext is RiskModelViewModel modelVM)
                {
                    var facts = modelVM.Facts?.ToArray();
                    if (facts?.Any() ?? false)
                    {
                        var associatedFacts = nodeVM.Facts?.OfType<LinkedFactViewModel>()?.ToArray();
                        var notAssociatedFacts = facts
                            .Where(x => !associatedFacts?.Any(y => y.LinkedFact.Id == x.Id) ?? true)
                            .ToArray();

                        var dialog = new AssociateFact(nodeVM, notAssociatedFacts);
                        if (dialog.ShowDialog() ?? false)
                        {
                            var selectedFact = dialog.SelectedFact;
                            if (selectedFact != null)
                            {
                                nodeVM.AddFact(selectedFact);
                            }
                        }
                    }
                }
            }
        }

        private void Item_Delete(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && DataContext is RiskModelViewModel model)
            {
                if (menuItem.Tag is MitigatedRiskViewModel mrVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete Risk '{mrVM.Name}'?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        model.RemoveRisk(mrVM);
                    }
                }
                else if (menuItem.Tag is MitigationCostViewModel mcVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete Mitigation '{mcVM.Name}'?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        model.RemoveMitigation(mcVM);
                    }
                }
                else if (menuItem.Tag is FactViewModel factVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete Mitigation '{factVM.Name}'?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        model.RemoveFact(factVM);
                    }
                }
                else if (menuItem.Tag is LinkedNodeViewModel lnVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete the link to {lnVM.NodeType} '{lnVM.Name}'?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        lnVM.Delete();
                    }
                }
                else if (menuItem.Tag is LinkedFactViewModel lfVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete the link to Fact '{lfVM.Name}'?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        lfVM.Delete();
                    }
                }
                else if (menuItem.Tag is AppliedMitigationViewModel amVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete applied Mitigation '{amVM.Name}'?",
                        "Confirm Disassociation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        amVM.Delete();
                    }
                }
                else if (menuItem.Tag is FactAnalyzerViewModel faVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete Fact Analyzer '{faVM.Name}'?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        model.RemoveFactAnalyzer(faVM);
                    }
                }
                else if (menuItem.Tag is NodeViewModel nodeVM)
                {
                    if (MessageBox.Show($"Are you sure you want to delete {nodeVM.NodeType} '{nodeVM.Name}'?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        nodeVM.Delete();
                    }
                }
            }
        }

        private void Item_AddChildFactAnalyzer(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is FactAnalyzerViewModel faVM && DataContext is RiskModelViewModel modelVM)
            {
                if (faVM.Parent is FactAnalyzerViewModel parent && parent.Parent is FactAnalyzerViewModel)
                {
                    MessageBox.Show("Fact Analyzers in QRiskTree Editor can only have two levels of depth.\nYou cannot add a child to this Fact Analyzer.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    faVM.AddFactAnalyzer("New Fact Analyzer");
                }
            }
        }

        private void Item_CalculateFactAnalyzer(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is FactAnalyzerViewModel faVM && DataContext is RiskModelViewModel modelVM)
            {
                if (faVM.Node.CanBeSimulated(out var node))
                {
                    AppendText("# Calculating Fact Analyzer");
                    AppendText($"**Model:** {modelVM.Properties.Name}");
                    AppendText();
                    AppendText($"**Created on:** {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}");
                    AppendText();
                    if (faVM.Node.SimulateAndGetSamples(out var samples, modelVM.Properties.MinPercentile,
                        modelVM.Properties.MaxPercentile, modelVM.Properties.Iterations) &&
                        samples != null && samples.Length == modelVM.Properties.Iterations)
                    {
                        AppendText($"## Fact Analyzer '{faVM.Name}'");
                        AppendText($"- Operation: {faVM.Operation.ToString()}");
                        AppendText($"- {modelVM.Properties.MinPercentile}th percentile: {faVM.Min.ToString("F2")}");
                        AppendText($"- Mode: {faVM.Mode.ToString("F2")}");
                        AppendText($"- {modelVM.Properties.MaxPercentile}th percentile: {faVM.Max.ToString("F2")}");
                        AppendText($"- Confidence: {faVM.Confidence}");

                        _chartFactAnalyzer.Plot(samples, modelVM.Properties.MinPercentile,
                            modelVM.Properties.MaxPercentile);
                        _tabFactAnalyzerResults.Visibility = Visibility.Visible;
                    }
                    AppendText();
                    AppendText();
                }
                else
                {
                    if (node != null)
                    {
                        MessageBox.Show($"Fact Analyzer '{faVM.Name}' is not valid because {node.GetType().Name.AddSpacesToCamelCase()} '{node.Name}' is not set.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Fact Analyzer '{faVM.Name}' is not valid.\nPlease check if it has all required children, or if you still must set its range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        #endregion

        #region Actions on the grids.
        private void Item_CreateRisk(object sender, RoutedEventArgs e)
        {
            _editCreateRisk_Click(sender, e);
        }

        private void Item_CreateMitigation(object sender, RoutedEventArgs e)
        {
            _editCreateMitigation_Click(sender, e);
        }

        private void Item_CreateFact(object sender, RoutedEventArgs e)
        {
            _editCreateFact_Click(sender, e);
        }

        private void Item_CreateFactWithNumericRange(object sender, RoutedEventArgs e)
        {
            _editCreateFactWithNumericRange_Click(sender, e);
        }

        private void Item_CreateFactWithMonetaryRange(object sender, RoutedEventArgs e)
        {
            _editCreateFactWithMonetaryRange_Click(sender, e);
        }

        private void Item_CreateFactWithFrequencyRange(object sender, RoutedEventArgs e)
        {
            _editCreateFactWithFrequencyRange_Click(sender, e);
        }

        private void Item_CreateFactWithPercentageRange(object sender, RoutedEventArgs e)
        {
            _editCreateFactWithPercentageRange_Click(sender, e);
        }

        private void Item_CreateFactAnalyzer(object sender, RoutedEventArgs e)
        {
            _editCreateFactAnalyzer_Click(sender, e);
        }
        #endregion

        private DataGridRow? GetDataGridRow(DependencyObject? current)
        {
            while (current != null && current is not DataGridRow)
                current = VisualTreeHelper.GetParent(current);
            return current as DataGridRow;
        }

        private DataGrid? GetRootDataGrid(DependencyObject? current)
        {
            if (current is Grid container && !string.IsNullOrEmpty(container.Name))
            {
                current = GetRootDataGrid(container.Children.OfType<DataGrid>().FirstOrDefault());
            }
            else
            {
                while (current != null &&
                    (current is not DataGrid || (current is DataGrid grid && string.IsNullOrEmpty(grid.Name))))
                    current = VisualTreeHelper.GetParent(current);
            }

            return current as DataGrid;
        }

        private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow? row = sender as DataGridRow;
            if (row != null)
            {
                row.IsSelected = true;
            }
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid)
            {
                grid.AddHandler(DataGridRow.ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            }
        }
        #endregion

        #region Markdown helper.
        private void AppendText(string? text = null)
        {
            _markdown.AppendLine(text);
            _output.Markdown = _markdown.ToString();
        }
        #endregion
    }
}
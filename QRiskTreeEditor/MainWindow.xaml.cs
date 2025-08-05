using QRiskTree.Engine;
using QRiskTree.Engine.ExtendedModel;
using QRiskTree.Engine.Facts;
using QRiskTree.Engine.Model;
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
using System.Xaml;
using System.Xml.XPath;
using TMFileParser;
using TMFileParser.Models.output;

namespace QRiskTreeEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _fileName = string.Empty;
        private QRiskTree.Engine.Range? _baseline;
        private double _outputHeight;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new RiskModelViewModel(RiskModel.Instance);
            _risks.AddHandler(DataGridRow.ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            _risksContainer.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            _mitigationsContainer.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            _factsContainer.AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OpeningContextMenu), false);
            SubscribeMitigatedRisks();
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
            if (MessageBox.Show("Are you sure you want to create a new model?\nUnsaved changes will be lost.", "Confirm New Model", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                RiskModel.Reset();
                FactsManager.Instance.Clear();
                _fileName = string.Empty;
                DataContext = new RiskModelViewModel(RiskModel.Instance);
            }
        }

        private void _fileOpen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open QRiskTree File",
                Filter = "QRiskTree files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _fileName = openFileDialog.FileName;
                    FactsManager.Instance.Clear();
                    if (RiskModel.Load(_fileName))
                    {
                        DataContext = new RiskModelViewModel(RiskModel.Instance);
                        SubscribeMitigatedRisks();

                        MessageBox.Show("File loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            else
            {
                RiskModel.Instance.Serialize(_fileName);
                MessageBox.Show("File saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void _fileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save QRiskTree File",
                Filter = "QRiskTree files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _fileName = saveFileDialog.FileName;
                    RiskModel.Instance.Serialize(_fileName);
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

        private void _clearOutput_Click(object sender, RoutedEventArgs e)
        {
            _output.Text = string.Empty;
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
                FactsManager.Instance.Import(openFileDialog.FileName);
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
                    FactsManager.Instance.Export(saveFileDialog.FileName);
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
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, _output.Text);
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
                    _output.AppendText("--- Calculating Baseline Risk ---\n");

                    _output.AppendText("Included Risks:\n");
                    foreach (var risk in risks)
                    {
                        _output.AppendText($"- Risk: {risk.Name}\n");
                    }

                    uint iterations = modelVM.Properties.Iterations;
                    Statistics.ResetSimulations();

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

                    _output.AppendText($"Risk for the baseline calculated in {stopwatch.ElapsedMilliseconds}ms ({Statistics.Simulations} * {iterations} samples generated).\n");

                    if (_baseline != null)
                    {
                        _output.AppendText($"- {modelVM.Properties.MinPercentile}th percentile: {_baseline.Min.ToString("C0")}\n");
                        _output.AppendText($"- Mode: {_baseline.Mode.ToString("C0")}\n");
                        _output.AppendText($"- {modelVM.Properties.MaxPercentile}th percentile: {_baseline.Max.ToString("C0")}\n");
                        _output.AppendText($"- Confidence: {_baseline.Confidence}\n");
                    }

                    _output.AppendText("--- Baseline Risk Calculation Completed ---\n\n");
                }
                else
                {
                    MessageBox.Show("No risk has been selected for baseline risk calculation.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void _calculateOptimalMitigations_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RiskModelViewModel modelVM)
            {
                var risks = modelVM.Risks?.OfType<MitigatedRiskViewModel>()?.Where(x => x.IsEnabled).ToArray();
                var mitigations = modelVM.Mitigations?.OfType<MitigationCostViewModel>()?.Where(x => x.IsEnabled).ToArray();
                if ((risks?.Any() ?? false) && (mitigations?.Any() ?? false))
                {
                    int countTreeSize = RecursiveCount(risks);
                    int countMitigations = mitigations.Length;
                    int countIterations = (countTreeSize + countMitigations) * ((1 << countMitigations) - 1);
                    int estimatedRequiredTime = countIterations * 20;
                    bool proceed;
                    if (estimatedRequiredTime > 30000)
                    {
                        proceed = MessageBox.Show($"The calculation of the optimal mitigations might require about {(estimatedRequiredTime / 60000).ToString("N1")} minutes. Do you want to proceed?", 
                            "Long running calculation", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
                    }
                    else
                    {
                        proceed = true;
                    }

                    if (proceed)
                    {
                        _output.AppendText("--- Calculating Optimal Mitigations Set ---\n");

                        _output.AppendText("Included Risks:\n");
                        foreach (var risk in risks)
                        {
                            _output.AppendText($"- Risk: {risk.Name}\n");
                        }

                        _output.AppendText("Included Mitigations:\n");
                        foreach (var mitigation in mitigations)
                        {
                            _output.AppendText($"- Mitigation: {mitigation.Name}\n");
                        }

                        uint iterations = modelVM.Properties.Iterations;
                        Statistics.ResetSimulations();

                        var optParameter = modelVM.Properties.OptimizationParameter;
                        var ignoreImplementationCosts = modelVM.Properties.IgnoreImplementationCosts;
                        var notText = ignoreImplementationCosts ? "not " : "";
                        _output.AppendText($"Optimization has been calculated on the {optParameter} parameter, and has {notText}considered the Implementation costs.\n");

                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        IEnumerable<MitigationCost>? optimized = null;
                        QRiskTree.Engine.Range? firstYearCosts = null;
                        QRiskTree.Engine.Range? followingYearsCosts = null;

                        try
                        {
                            Mouse.OverrideCursor = Cursors.Wait;
                            optimized = modelVM.Model.OptimizeMitigations(out firstYearCosts, out followingYearsCosts, 
                                iterations: iterations, optimizationParameter: optParameter, optimizeForFollowingYears: ignoreImplementationCosts);
                        }
                        finally
                        {
                            Mouse.OverrideCursor = null;
                            stopwatch.Stop();
                        }

                        _output.AppendText($"Optimization completed in {stopwatch.ElapsedMilliseconds}ms ({Statistics.Simulations} * {iterations} samples generated).\n");

                        if (firstYearCosts != null)
                        {
                            _output.AppendText("\nEstimation of the Minimal Overall Yearly Cost for the first year:\n");
                            _output.AppendText($"- {modelVM.Properties.MinPercentile}th percentile: {firstYearCosts.Min.ToString("C0")}");
                            if (_baseline != null)
                                _output.AppendText($" (saving {(_baseline.Min - firstYearCosts.Min).ToString("C0")}, equal to {((_baseline.Min - firstYearCosts.Min) / _baseline.Min).ToString("P2")})\n");
                            else
                                _output.AppendText("\n");
                            _output.AppendText($"- Mode: {firstYearCosts.Mode.ToString("C0")}");
                            if (_baseline != null)
                                _output.AppendText($" (saving {(_baseline.Mode - firstYearCosts.Mode).ToString("C0")}, equal to {((_baseline.Mode - firstYearCosts.Mode) / _baseline.Mode).ToString("P2")})\n");
                            else
                                _output.AppendText("\n");
                            _output.AppendText($"- {modelVM.Properties.MaxPercentile}th percentile: {firstYearCosts.Max.ToString("C0")}");
                            if (_baseline != null)
                                _output.AppendText($" (saving {(_baseline.Max - firstYearCosts.Max).ToString("C0")}, equal to {((_baseline.Max - firstYearCosts.Max) / _baseline.Max).ToString("P2")})\n");
                            else
                                _output.AppendText("\n");
                            _output.AppendText($"- Confidence: {firstYearCosts.Confidence}\n");
                        }

                        if (followingYearsCosts != null)
                        {
                            _output.AppendText("\nEstimation of the Minimal Overall Yearly Cost for the following years:\n");
                            _output.AppendText($"- {modelVM.Properties.MinPercentile}th percentile: {followingYearsCosts.Min.ToString("C0")}");
                            if (_baseline != null)
                                _output.AppendText($" (saving {(_baseline.Min - followingYearsCosts.Min).ToString("C0")}, equal to {((_baseline.Min - followingYearsCosts.Min) / _baseline.Min).ToString("P2")})\n");
                            else
                                _output.AppendText("\n");
                            _output.AppendText($"- Mode: {followingYearsCosts.Mode.ToString("C0")}");
                            if (_baseline != null)
                                _output.AppendText($" (saving {(_baseline.Mode - followingYearsCosts.Mode).ToString("C0")}, equal to {((_baseline.Mode - followingYearsCosts.Mode) / _baseline.Mode).ToString("P2")})\n");
                            else
                                _output.AppendText("\n");
                            _output.AppendText($"- {modelVM.Properties.MaxPercentile}th percentile: {followingYearsCosts.Max.ToString("C0")}");
                            if (_baseline != null)
                                _output.AppendText($" (saving {(_baseline.Max - followingYearsCosts.Max).ToString("C0")}, equal to {((_baseline.Max - followingYearsCosts.Max) / _baseline.Max).ToString("P2")})\n");
                            else
                                _output.AppendText("\n");
                            _output.AppendText($"- Confidence: {followingYearsCosts.Confidence}\n");
                        }

                        if (optimized?.Any() ?? false)
                        {
                            _output.AppendText("\nMitigations to be applied:\n");
                            foreach (var mitigation in optimized)
                            {
                                _output.AppendText($"- {mitigation.Name}\n");
                                _output.AppendText($"  - Implementation Costs: {mitigation.Min} - {mitigation.Mode} - {mitigation.Max} ({mitigation.Confidence})\n");
                                if (mitigation.OperationCosts != null)
                                {
                                    _output.AppendText($"  - Operation Costs: {mitigation.OperationCosts.Min} - {mitigation.OperationCosts.Mode} - {mitigation.OperationCosts.Max} ({mitigation.OperationCosts.Confidence})\n");
                                }
                            }
                        }

                        _output.AppendText("--- Calculation of the Optimal Set of Mitigations Completed ---\n\n");
                    }
                }
                else
                {
                    MessageBox.Show("No risks or mitigations selected for optimized set calculation.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private int RecursiveCount(IEnumerable<NodeViewModel> nodes)
        {
            int count = 0;

            if (nodes?.Any() ?? false)
            {
                foreach (var node in nodes)
                {
                    if (node.IsSetByUser)
                        count++;
                    var children = node.Components?.OfType<NodeViewModel>()?.ToArray();
                    if (children?.Any() ?? false)
                    {
                        count += RecursiveCount(children);
                    }
                }
            }

            return count;
        }
        #endregion
        #endregion

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

            if (result)
            {
                e.Handled = true;
            }
        }

        private bool OpenContextMenuForRow(DataGridRow row)
        {
            var result = false;

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

                if (DataContext is RiskModelViewModel modelVM)
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

            if (row.DataContext is NodeViewModel nodeVM)
            {
                var totalFacts = FactsManager.Instance.Facts?.Count() ?? 0;
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
                    menuItem.Click -= Item_CreateRisk;
                    menuItem.Click -= Item_CreateMitigation;
                    menuItem.Click -= Item_CreateFact;
                    menuItem.Click -= Item_CreateFactWithMonetaryRange;
                    menuItem.Click -= Item_CreateFactWithFrequencyRange;
                    menuItem.Click -= Item_CreateFactWithPercentageRange;
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
    }
}
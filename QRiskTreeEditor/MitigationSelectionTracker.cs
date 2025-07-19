using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QRiskTreeEditor
{
    public static class MitigationSelectionTracker
    {
        private static DataGrid? _lastFocused;

        public static readonly DependencyProperty TrackSelectionProperty =
            DependencyProperty.RegisterAttached(
                "TrackSelection",
                typeof(bool),
                typeof(MitigationSelectionTracker),
                new PropertyMetadata(false, OnTrackSelectionChanged));

        public static bool GetTrackSelection(DependencyObject obj) => (bool)obj.GetValue(TrackSelectionProperty);
        public static void SetTrackSelection(DependencyObject obj, bool value) => obj.SetValue(TrackSelectionProperty, value);

        private static void OnTrackSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid && e.NewValue is true)
            {
                dataGrid.PreviewMouseDown += (s, args) =>
                {
                    _lastFocused = dataGrid;
                };

                dataGrid.SelectionChanged += (s, args) =>
                {
                    // Only update if this DataGrid has keyboard focus
                    if (_lastFocused != dataGrid || dataGrid.SelectedItem == null)
                        return;
                    
                    var rootDataContext = FindRootDataContext(dataGrid);
                    if (rootDataContext is IMitigationSelectionReceiver receiver)
                    {
                        receiver.SelectedMitigation = dataGrid.SelectedItem;
                    }
                };
            }
        }

        private static object? FindRootDataContext(DependencyObject current)
        {
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is IMitigationSelectionReceiver)
                    return fe.DataContext;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}

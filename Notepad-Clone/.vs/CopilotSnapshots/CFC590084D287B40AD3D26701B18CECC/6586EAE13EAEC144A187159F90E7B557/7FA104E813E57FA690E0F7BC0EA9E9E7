using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Tema1_MVP.Services;
using Tema1_MVP.ViewModels;
using TextBox = System.Windows.Controls.TextBox;
using ContentPresenter = System.Windows.Controls.ContentPresenter;
using Button = System.Windows.Controls.Button;

namespace Tema1_MVP
{
    /// <summary>
    /// Code-behind for MainWindow.
    /// Responsibilities are strictly View-level:
    ///   - Set DataContext
    ///   - Subscribe to ViewModel events that require direct TextBox access (selection/caret)
    ///   - Forward raw UI events (tab-close button, TreeView double-click) to the ViewModel
    /// No business logic lives here.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel VM => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();

            var dialogService = new DialogService();
            DataContext = new MainViewModel(dialogService);

            // Subscribe to ViewModel events that require TextBox access
            VM.ScrollToMatch   += OnScrollToMatch;
            VM.RequestGoToLine += OnGoToLine;
        }

        // ── Window closing ────────────────────────────────────────────────────

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Cancel the OS close; let ExitCommand handle save-confirmation and Shutdown()
            e.Cancel = true;
            VM.ExitCommand.Execute(null);
        }

        // ── Tab strip close button ────────────────────────────────────────────

        /// <summary>
        /// The ✕ Button inside each tab header has Tag="{Binding}" (the DocumentViewModel).
        /// We retrieve it here and ask the ViewModel to close that document.
        /// </summary>
        private void TabCloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: DocumentViewModel doc })
                VM.CloseDocumentPublic(doc);
        }

        // ── Folder TreeView ───────────────────────────────────────────────────

        /// <summary>Keeps ViewModel.SelectedTreeItem in sync with the TreeView selection.</summary>
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileTreeItemViewModel item)
                VM.SelectedTreeItem = item;
        }

        /// <summary>Double-clicking a file node opens it in a new tab.</summary>
        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VM.SelectedTreeItem is { IsDirectory: false })
                VM.TreeOpenFileCommand.Execute(null);
        }

        // ── ViewModel events that require direct TextBox manipulation ─────────

        /// <summary>
        /// Scrolls the active TextBox to the character offset of the search match and selects it.
        /// TextBox lives inside a DataTemplate, so we walk the visual tree to find it.
        /// </summary>
        private void OnScrollToMatch(DocumentViewModel doc, int charIndex)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                var tb = GetActiveTextBox();
                if (tb is null) return;
                tb.Focus();
                int safeLen = Math.Min(VM.SearchText.Length, tb.Text.Length - charIndex);
                tb.Select(charIndex, Math.Max(0, safeLen));
                tb.ScrollToLine(tb.GetLineIndexFromCharacterIndex(charIndex));
            });
        }

        /// <summary>
        /// Moves the caret to the start of the requested 1-based line number.
        /// </summary>
        private void OnGoToLine(DocumentViewModel doc, int lineNumber)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                var tb = GetActiveTextBox();
                if (tb is null) return;

                int zeroLine = Math.Max(0, lineNumber - 1);
                if (zeroLine >= tb.LineCount) zeroLine = tb.LineCount - 1;

                int charIdx = tb.GetCharacterIndexFromLineIndex(zeroLine);
                tb.Focus();
                tb.Select(charIdx, 0);
                tb.ScrollToLine(zeroLine);
            });
        }

        /// <summary>
        /// Resolves the TextBox inside the currently selected tab via the visual tree.
        /// </summary>
        private TextBox? GetActiveTextBox()
        {
            if (MainTabControl.SelectedItem is not DocumentViewModel) return null;

            if (MainTabControl.Template.FindName("PART_SelectedContentHost", MainTabControl)
                is not ContentPresenter cp) return null;

            cp.ApplyTemplate();
            return FindVisualChild<TextBox>(cp);
        }

        /// <summary>Depth-first visual-tree search for a child of type T.</summary>
        private static T? FindVisualChild<T>(System.Windows.DependencyObject parent)
            where T : System.Windows.DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var result = FindVisualChild<T>(child);
                if (result is not null) return result;
            }
            return null;
        }
    }
}
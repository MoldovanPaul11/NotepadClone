using System.Windows;
using Tema1_MVP.Views;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Application = System.Windows.Application;

namespace Tema1_MVP.Services
{
    public class DialogService : IDialogService
    {
        public string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string title = "Open")
        {
            var dlg = new OpenFileDialog
            {
                Filter = filter,
                Title = title
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string? ShowSaveFileDialog(string defaultFileName = "",
            string filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            string title = "Save As")
        {
            var dlg = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                Title = title
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string? ShowFolderBrowserDialog(string description = "Select Folder")
        {
            // WPF doesn't have a built-in folder browser; use the Windows Forms one.
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true
            };
            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? dialog.SelectedPath
                : null;
        }

        public MessageBoxResult ShowConfirmation(string message, string title = "Confirm")
            => MessageBox.Show(message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        public void ShowMessage(string message, string title = "Information")
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public void ShowAbout()
        {
            var win = new AboutWindow();
            win.ShowDialog();
        }

        public void ShowFindReplace(object dataContext)
        {
            // Re-use an existing window or create a new one
            foreach (Window w in Application.Current.Windows)
            {
                if (w is FindReplaceWindow existing)
                {
                    existing.Activate();
                    return;
                }
            }
            var win = new FindReplaceWindow { DataContext = dataContext };
            win.Show();
        }
    }
}

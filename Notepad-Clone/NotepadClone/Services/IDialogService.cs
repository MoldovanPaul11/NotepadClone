using System.Windows;

namespace Tema1_MVP.Services
{
    public interface IDialogService
    {
        /// <summary>Shows an Open File dialog. Returns the chosen path or null.</summary>
        string? ShowOpenFileDialog(string filter = "All files (*.*)|*.*", string title = "Open");

        /// <summary>Shows a Save File dialog. Returns the chosen path or null.</summary>
        string? ShowSaveFileDialog(string defaultFileName = "", string filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", string title = "Save As");

        /// <summary>Shows a folder-browser dialog. Returns chosen path or null.</summary>
        string? ShowFolderBrowserDialog(string description = "Select Folder");

        /// <summary>Shows a Yes/No/Cancel message box. Returns the result.</summary>
        MessageBoxResult ShowConfirmation(string message, string title = "Confirm");

        /// <summary>Shows an information message box.</summary>
        void ShowMessage(string message, string title = "Information");

        /// <summary>Shows the About window.</summary>
        void ShowAbout();

        /// <summary>Shows the Find/Replace window, passing the main ViewModel as data context.</summary>
        void ShowFindReplace(object dataContext);
    }
}

using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Tema1_MVP.Commands;
using Tema1_MVP.Models;
using Tema1_MVP.Services;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using MessageBoxResult = System.Windows.MessageBoxResult;
using ApplicationCommands = System.Windows.Input.ApplicationCommands;

namespace Tema1_MVP.ViewModels
{
    /// <summary>
    /// Main ViewModel orchestrating tabs, folder explorer, search/replace and all menu commands.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private int _newFileCounter = 0;
        private DocumentViewModel? _selectedDocument;
        private FileTreeItemViewModel? _selectedTreeItem;
        private FileTreeItemViewModel? _copiedFolder;   // folder held for Paste operation
        private bool _isFolderExplorerVisible = true;
        private bool _searchInAllTabs = true;

        // Search & Replace fields
        private string _searchText = string.Empty;
        private string _replaceText = string.Empty;
        private int _goToLine = 1;

        #endregion

        #region Collections

        public ObservableCollection<DocumentViewModel> Documents { get; } = new();
        public ObservableCollection<FileTreeItemViewModel> TreeRoots { get; } = new();

        #endregion

        #region Properties

        public DocumentViewModel? SelectedDocument
        {
            get => _selectedDocument;
            set => SetProperty(ref _selectedDocument, value);
        }

        public FileTreeItemViewModel? SelectedTreeItem
        {
            get => _selectedTreeItem;
            set => SetProperty(ref _selectedTreeItem, value);
        }

        public bool IsFolderExplorerVisible
        {
            get => _isFolderExplorerVisible;
            set => SetProperty(ref _isFolderExplorerVisible, value);
        }

        public bool SearchInAllTabs
        {
            get => _searchInAllTabs;
            set => SetProperty(ref _searchInAllTabs, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string ReplaceText
        {
            get => _replaceText;
            set => SetProperty(ref _replaceText, value);
        }

        public int GoToLine
        {
            get => _goToLine;
            set => SetProperty(ref _goToLine, value);
        }

        /// <summary>True when there is a folder copied into clipboard for paste.</summary>
        public bool HasCopiedFolder => _copiedFolder is not null;

        #endregion

        #region Commands – File

        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand CloseCurrentCommand { get; }
        public ICommand CloseAllCommand { get; }
        public ICommand ExitCommand { get; }

        #endregion

        #region Commands – Search

        public ICommand FindCommand { get; }
        public ICommand ReplaceCommand { get; }
        public ICommand ReplaceAllCommand { get; }

        #endregion

        #region Commands – View

        public ICommand ToggleFolderExplorerCommand { get; }
        public ICommand OpenFolderCommand { get; }

        #endregion

        #region Commands – Help

        public ICommand AboutCommand { get; }

        #endregion

        #region Commands – Folder Explorer Context Menu

        public ICommand TreeNewFileCommand { get; }
        public ICommand TreeCopyPathCommand { get; }
        public ICommand TreeCopyFolderCommand { get; }
        public ICommand TreePasteFolderCommand { get; }
        public ICommand TreeOpenFileCommand { get; }

        #endregion

        #region Commands – Edit (bonus)

        public ICommand CopyCommand { get; }
        public ICommand CutCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand ToUpperCommand { get; }
        public ICommand ToLowerCommand { get; }

        #endregion

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            // ?? File ??????????????????????????????????????????????????????????
            NewCommand = new RelayCommand(NewDocument);
            OpenCommand = new RelayCommand(OpenDocument);
            SaveCommand = new RelayCommand(SaveCurrent, () => SelectedDocument is not null);
            SaveAsCommand = new RelayCommand(SaveAsCurrent, () => SelectedDocument is not null);
            CloseCurrentCommand = new RelayCommand(CloseCurrent, () => SelectedDocument is not null);
            CloseAllCommand = new RelayCommand(CloseAll, () => Documents.Count > 0);
            ExitCommand = new RelayCommand(ExitApp);

            // ?? Search ????????????????????????????????????????????????????????
            FindCommand = new RelayCommand(() => _dialogService.ShowFindReplace(this));
            ReplaceCommand = new RelayCommand(DoReplace, () => !string.IsNullOrEmpty(SearchText));
            ReplaceAllCommand = new RelayCommand(DoReplaceAll, () => !string.IsNullOrEmpty(SearchText));

            // ?? View ??????????????????????????????????????????????????????????
            ToggleFolderExplorerCommand = new RelayCommand(() => IsFolderExplorerVisible = !IsFolderExplorerVisible);
            OpenFolderCommand = new RelayCommand(OpenFolder);

            // ?? Help ??????????????????????????????????????????????????????????
            AboutCommand = new RelayCommand(_dialogService.ShowAbout);

            // ?? Tree context menu ?????????????????????????????????????????????
            TreeNewFileCommand = new RelayCommand(TreeNewFile, () => SelectedTreeItem?.IsDirectory ?? false);
            TreeCopyPathCommand = new RelayCommand(TreeCopyPath, () => SelectedTreeItem is not null);
            TreeCopyFolderCommand = new RelayCommand(TreeCopyFolder, () => SelectedTreeItem?.IsDirectory ?? false);
            TreePasteFolderCommand = new RelayCommand(TreePasteFolder, () => HasCopiedFolder && (SelectedTreeItem?.IsDirectory ?? false));
            TreeOpenFileCommand = new RelayCommand(TreeOpenFile, () => SelectedTreeItem is not null && !SelectedTreeItem.IsDirectory);

            // ?? Edit (bonus) ??????????????????????????????????????????????????
            CopyCommand = new RelayCommand(() => ApplicationCommands.Copy.Execute(null, null));
            CutCommand = new RelayCommand(() => ApplicationCommands.Cut.Execute(null, null));
            PasteCommand = new RelayCommand(() => ApplicationCommands.Paste.Execute(null, null));
            ToUpperCommand = new RelayCommand(ConvertToUpper);
            ToLowerCommand = new RelayCommand(ConvertToLower);

            // Start with one blank document
            NewDocument();
        }

        #region File Logic

        private void NewDocument()
        {
            _newFileCounter++;
            var model = new DocumentModel
            {
                Title = $"File {_newFileCounter}",
                Content = string.Empty
            };
            var vm = new DocumentViewModel(model);
            vm.IsModified = false;   // brand-new file is not considered dirty yet
            Documents.Add(vm);
            SelectedDocument = vm;
        }

        private void OpenDocument()
        {
            var path = _dialogService.ShowOpenFileDialog(
                "Text files (*.txt)|*.txt|All files (*.*)|*.*", "Open File");
            if (path is null) return;

            // If the file is already open, just switch to that tab
            foreach (var doc in Documents)
            {
                if (string.Equals(doc.FilePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedDocument = doc;
                    return;
                }
            }

            var content = File.ReadAllText(path);
            var model = new DocumentModel
            {
                FilePath = path,
                Title = Path.GetFileName(path),
                Content = content
            };
            var vm = new DocumentViewModel(model) { };
            vm.IsModified = false;
            Documents.Add(vm);
            SelectedDocument = vm;
        }

        private void SaveCurrent()
        {
            if (SelectedDocument is null) return;
            if (SelectedDocument.FilePath is null)
                SaveAsCurrent();
            else
                WriteFile(SelectedDocument, SelectedDocument.FilePath);
        }

        private void SaveAsCurrent()
        {
            if (SelectedDocument is null) return;
            var path = _dialogService.ShowSaveFileDialog(
                SelectedDocument.Title,
                "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                "Save As");
            if (path is null) return;
            WriteFile(SelectedDocument, path);
        }

        private void WriteFile(DocumentViewModel doc, string path)
        {
            File.WriteAllText(path, doc.Content);
            doc.FilePath = path;
            doc.IsModified = false;
        }

        private void CloseCurrent()
        {
            if (SelectedDocument is null) return;
            CloseDocument(SelectedDocument);
        }

        /// <summary>
        /// Public surface called by the View's tab-close button (which has the DocumentViewModel
        /// as its Tag and cannot use a command parameter cleanly inside a DataTemplate).
        /// </summary>
        public void CloseDocumentPublic(DocumentViewModel doc) => CloseDocument(doc);

        private void CloseAll()
        {
            // Iterate over a copy so we can remove during iteration
            foreach (var doc in Documents.ToList())
            {
                if (!CloseDocument(doc))
                    return;   // user cancelled
            }
        }

        /// <summary>
        /// Attempts to close a document.
        /// Returns false when the user pressed Cancel (for CloseAll to abort).
        /// </summary>
        private bool CloseDocument(DocumentViewModel doc)
        {
            if (doc.IsModified)
            {
                var result = _dialogService.ShowConfirmation(
                    $"'{doc.Title}' has unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes");

                if (result == MessageBoxResult.Yes)
                    SaveDocument(doc);
                else if (result == MessageBoxResult.Cancel)
                    return false;
            }

            Documents.Remove(doc);

            // Select the last remaining tab
            if (Documents.Count > 0)
                SelectedDocument = Documents[^1];

            return true;
        }

        private void SaveDocument(DocumentViewModel doc)
        {
            if (doc.FilePath is null)
            {
                var path = _dialogService.ShowSaveFileDialog(doc.Title,
                    "Text files (*.txt)|*.txt|All files (*.*)|*.*", "Save As");
                if (path is null) return;
                WriteFile(doc, path);
            }
            else
            {
                WriteFile(doc, doc.FilePath);
            }
        }

        private void ExitApp()
        {
            foreach (var doc in Documents.ToList())
            {
                if (!CloseDocument(doc))
                    return;
            }
            Application.Current.Shutdown();
        }

        #endregion

        #region Search & Replace Logic

        /// <summary>
        /// Finds the first occurrence of SearchText in the selected document(s) and highlights it.
        /// Because WPF TextBox doesn't expose selection from ViewModel, we raise a custom event
        /// that the View subscribes to.
        /// </summary>
        public event Action<DocumentViewModel, int>? ScrollToMatch;

        /// <summary>Raised by Find/Replace window to ask the View to scroll to a line number.</summary>
        public event Action<DocumentViewModel, int>? RequestGoToLine;

        /// <summary>Called by the Find/Replace View to trigger line navigation in the editor.</summary>
        public void RaiseGoToLine(DocumentViewModel doc, int lineNumber)
            => RequestGoToLine?.Invoke(doc, lineNumber);

        public void DoFind()
        {
            if (string.IsNullOrEmpty(SearchText)) return;
            var targets = SearchInAllTabs ? Documents : (IEnumerable<DocumentViewModel>)[SelectedDocument!];
            foreach (var doc in targets)
            {
                int idx = doc.Content.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    SelectedDocument = doc;
                    ScrollToMatch?.Invoke(doc, idx);
                    return;
                }
            }
            _dialogService.ShowMessage($"'{SearchText}' not found.", "Find");
        }

        private void DoReplace()
        {
            if (string.IsNullOrEmpty(SearchText)) return;
            var targets = SearchInAllTabs ? Documents : (IEnumerable<DocumentViewModel>)[SelectedDocument!];
            foreach (var doc in targets)
            {
                int idx = doc.Content.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    doc.Content = doc.Content.Remove(idx, SearchText.Length).Insert(idx, ReplaceText);
                    SelectedDocument = doc;
                    return;
                }
            }
            _dialogService.ShowMessage($"'{SearchText}' not found.", "Replace");
        }

        private void DoReplaceAll()
        {
            if (string.IsNullOrEmpty(SearchText)) return;
            int total = 0;
            var targets = SearchInAllTabs ? Documents : (IEnumerable<DocumentViewModel>)[SelectedDocument!];

            foreach (var doc in targets)
            {
                // Replace all occurrences in this document (case-insensitive)
                int count = 0;
                string result = System.Text.RegularExpressions.Regex.Replace(
                    doc.Content,
                    System.Text.RegularExpressions.Regex.Escape(SearchText),
                    ReplaceText,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                    TimeSpan.FromSeconds(5));
                count = System.Text.RegularExpressions.Regex
                    .Matches(doc.Content, System.Text.RegularExpressions.Regex.Escape(SearchText),
                             System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
                doc.Content = result;
                total += count;
            }
            _dialogService.ShowMessage($"Replaced {total} occurrence(s).", "Replace All");
        }

        #endregion

        #region Folder Explorer Logic

        private void OpenFolder()
        {
            var path = _dialogService.ShowFolderBrowserDialog("Select a folder to explore");
            if (path is null) return;

            var model = new FileTreeItemModel
            {
                Name = Path.GetFileName(path) is { Length: > 0 } n ? n : path,
                FullPath = path,
                IsDirectory = true
            };
            TreeRoots.Add(new FileTreeItemViewModel(model));
        }

        private void TreeNewFile()
        {
            if (SelectedTreeItem is null || !SelectedTreeItem.IsDirectory) return;
            var path = _dialogService.ShowSaveFileDialog("NewFile.txt",
                "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                "Create New File in Folder");
            if (path is null) return;

            // Ensure the file is created in the selected tree folder
            string name = Path.GetFileName(path);
            string target = Path.Combine(SelectedTreeItem.FullPath, name);
            File.WriteAllText(target, string.Empty);
            SelectedTreeItem.Refresh();
        }

        private void TreeCopyPath()
        {
            if (SelectedTreeItem is null) return;
            Clipboard.SetText(SelectedTreeItem.FullPath);
        }

        private void TreeCopyFolder()
        {
            if (SelectedTreeItem is null || !SelectedTreeItem.IsDirectory) return;
            _copiedFolder = SelectedTreeItem;
            OnPropertyChanged(nameof(HasCopiedFolder));
        }

        /// <summary>
        /// Recursively copies the stored folder into the selected destination directory.
        /// </summary>
        private void TreePasteFolder()
        {
            if (_copiedFolder is null || SelectedTreeItem is null || !SelectedTreeItem.IsDirectory) return;

            string srcRoot = _copiedFolder.FullPath;
            string dstRoot = Path.Combine(SelectedTreeItem.FullPath, Path.GetFileName(srcRoot));

            CopyDirectoryRecursive(srcRoot, dstRoot);
            SelectedTreeItem.Refresh();
        }

        /// <summary>
        /// Recursively copies a directory tree from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        private static void CopyDirectoryRecursive(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            // Copy all files in this directory
            foreach (var file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);

            // Recursively copy every sub-directory
            foreach (var dir in Directory.GetDirectories(source))
            {
                string subDirName = Path.GetFileName(dir);
                CopyDirectoryRecursive(dir, Path.Combine(destination, subDirName));
            }
        }

        private void TreeOpenFile()
        {
            if (SelectedTreeItem is null || SelectedTreeItem.IsDirectory) return;
            OpenFileByPath(SelectedTreeItem.FullPath);
        }

        /// <summary>Opens a file in a new tab (or switches to an existing tab if already open).</summary>
        public void OpenFileByPath(string path)
        {
            foreach (var doc in Documents)
            {
                if (string.Equals(doc.FilePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedDocument = doc;
                    return;
                }
            }

            try
            {
                var content = File.ReadAllText(path);
                var model = new DocumentModel
                {
                    FilePath = path,
                    Title = Path.GetFileName(path),
                    Content = content
                };
                var vm = new DocumentViewModel(model);
                vm.IsModified = false;
                Documents.Add(vm);
                SelectedDocument = vm;
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Could not open file:\n{ex.Message}", "Error");
            }
        }

        #endregion

        #region Edit / Bonus Logic

        private void ConvertToUpper()
        {
            // Selection manipulation is triggered from the View via ApplicationCommands
            // For full ViewModel-controlled selection we would need a custom attached behavior.
            // Here we convert the entire content of the current document.
            if (SelectedDocument is null) return;
            SelectedDocument.Content = SelectedDocument.Content.ToUpper();
        }

        private void ConvertToLower()
        {
            if (SelectedDocument is null) return;
            SelectedDocument.Content = SelectedDocument.Content.ToLower();
        }

        #endregion
    }
}

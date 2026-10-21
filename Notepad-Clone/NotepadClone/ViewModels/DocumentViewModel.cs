using Tema1_MVP.Models;

namespace Tema1_MVP.ViewModels
{
    /// <summary>
    /// ViewModel for a single open document / tab.
    /// </summary>
    public class DocumentViewModel : BaseViewModel
    {
        private string _content = string.Empty;
        private bool _isModified;
        private string? _filePath;
        private string _title = string.Empty;

        public DocumentModel Model { get; }

        public DocumentViewModel(DocumentModel model)
        {
            Model = model;
            _filePath = model.FilePath;
            _content = model.Content;
            _title = model.Title;
        }

        /// <summary>Disk path – null for unsaved new files.</summary>
        public string? FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    Model.FilePath = value;
                    if (value is not null)
                        Title = System.IO.Path.GetFileName(value);
                    OnPropertyChanged(nameof(TabHeader));
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    Model.Title = value;
                    OnPropertyChanged(nameof(TabHeader));
                }
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                if (SetProperty(ref _content, value))
                {
                    Model.Content = value;
                    IsModified = true;
                }
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set
            {
                if (SetProperty(ref _isModified, value))
                    OnPropertyChanged(nameof(TabHeader));
            }
        }

        /// <summary>
        /// Header shown on the tab; appends '*' when the document has unsaved changes.
        /// </summary>
        public string TabHeader => IsModified ? $"* {Title}" : Title;
    }
}

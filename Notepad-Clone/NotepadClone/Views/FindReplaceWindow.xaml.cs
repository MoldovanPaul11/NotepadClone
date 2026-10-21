using System.Windows;
using Tema1_MVP.ViewModels;

namespace Tema1_MVP.Views
{
    public partial class FindReplaceWindow : Window
    {
        public FindReplaceWindow()
        {
            InitializeComponent();
        }

        private MainViewModel? VM => DataContext as MainViewModel;

        private void Find_Click(object sender, RoutedEventArgs e) => VM?.DoFind();

        private void GoToLine_Click(object sender, RoutedEventArgs e)
        {
            if (VM?.SelectedDocument is null) return;
            VM.RaiseGoToLine(VM.SelectedDocument, VM.GoToLine);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}

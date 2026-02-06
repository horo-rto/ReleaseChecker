using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReleaseChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Accept if at least one path is a directory
                bool hasDir = false;
                foreach (var p in paths)
                {
                    if (Directory.Exists(p)) { hasDir = true; break; }
                }
                e.Effects = hasDir ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    // avoid duplicates
                    if (!FoldersListBox.Items.Contains(p))
                        FoldersListBox.Items.Add(p);
                    // Placeholder: integrate with existing logic to process folder `p`
                }
            }
            e.Handled = true;
        }
    }
}
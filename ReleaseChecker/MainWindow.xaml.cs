using MediaInfoLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IOPath = System.IO.Path;

namespace ReleaseChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<FileRow> _rows = new ObservableCollection<FileRow>();
        Style cellTextStyle;
        Style cellPaddingStyle;
        public MainWindow()
        {
            InitializeComponent();
            FilesDataGrid.ItemsSource = _rows;

            this.MaxWidth = SystemParameters.WorkArea.Width;
            this.MaxHeight = SystemParameters.WorkArea.Height;

            cellTextStyle = (Style)FindResource("CellTextStyle");
            cellPaddingStyle = (Style)FindResource("CellPaddingStyle");
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            var data = ReadData(paths);
            UpdateUI(data);

            e.Handled = true;
        }

        private List<(string Rel, MediaFileInfo Mfi)> ReadData(string[] paths)
        {
            var analyzed = new List<(string Rel, MediaFileInfo Mfi)>();

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    var rel = IOPath.GetFileName(path);
                    var mfi = new MediaFileInfo(path);
                    analyzed.Add((rel, mfi));
                }

                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).OrderBy(f => f);
                    foreach (var file in files)
                    {
                        var rel = IOPath.GetRelativePath(path, file);
                        var mfi = new MediaFileInfo(file);
                        analyzed.Add((rel, mfi));
                    }
                }
            }
            return analyzed;
        }

        private void UpdateUI(List<(string Rel, MediaFileInfo Mfi)> analyzed)
        {
            _rows.Clear();

            foreach (var entry in analyzed)
            {
                var fr = new FileRow();
                var mfi = entry.Mfi;

                fr.FileName = entry.Rel;
                fr.Fields["FileName"] = entry.Rel;

                // first video stream summary or empty
                if (mfi != null && mfi.VideoStreams != null && mfi.VideoStreams.Count > 0)
                {
                    fr.Fields["Video"] = mfi.VideoStreams[0];
                }
                else fr.Fields["Video"] = string.Empty;

                // first audio stream summary or empty
                if (mfi != null && mfi.AudioStreams != null && mfi.AudioStreams.Count > 0)
                {
                    fr.Fields["Audio"] = mfi.AudioStreams[0];
                }
                else fr.Fields["Audio"] = string.Empty;

                _rows.Add(fr);
            }
        }
    }

    public class FileRow
    {
        public string FileName { get; set; }
        public Dictionary<string, object> Fields { get; } = new Dictionary<string, object>();
        public object this[string key]
        {
            get
            {
                if (Fields.TryGetValue(key, out var v)) return v;
                return string.Empty;
            }
        }
    }
}
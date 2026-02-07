using MediaInfoLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        public MainWindow()
        {
            InitializeComponent();
            FilesDataGrid.ItemsSource = _rows;

            // cap window size to the screen work area so SizeToContent won't exceed the screen
            this.MaxWidth = SystemParameters.WorkArea.Width;
            this.MaxHeight = SystemParameters.WorkArea.Height;

            // initialize DataGrid with a File column; video/audio columns are added dynamically after analysis
            FilesDataGrid.Columns.Clear();
            var textStyle = (Style)FindResource("CellTextStyle");
            var cellStyle = (Style)FindResource("CellPaddingStyle");
            var fileCol = new DataGridTextColumn
            {
                Header = "File",
                Binding = new System.Windows.Data.Binding("FileName"),
                ElementStyle = textStyle,
                CellStyle = cellStyle,
                Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells),
                MinWidth = 200
            };
            FilesDataGrid.Columns.Add(fileCol);
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
            FilesDataGrid.Columns.Clear();

            int maxVideo = analyzed.Max(a => a.Mfi?.VideoStreams?.Count ?? 0);
            int maxAudio = analyzed.Max(a => a.Mfi?.AudioStreams?.Count ?? 0);

            // rebuild DataGrid columns: keep File column first
            var cellTextStyle = (Style)FindResource("CellTextStyle");
            var cellPaddingStyle = (Style)FindResource("CellPaddingStyle");
            FilesDataGrid.Columns.Add(new DataGridTextColumn { Header = "File", Binding = new Binding("FileName"), Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells), MinWidth = 200, ElementStyle = cellTextStyle, CellStyle = cellPaddingStyle });

            for (int i = 0; i < maxVideo; i++)
            {
                var key = $"Video {i + 1}";
                var col = new DataGridTextColumn { 
                    Header = key, 
                    Binding = new Binding($"[{key}]"), 
                    Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells), 
                    MinWidth = 120, 
                    ElementStyle = cellTextStyle, 
                    CellStyle = cellPaddingStyle
                };
                FilesDataGrid.Columns.Add(col);
            }

            for (int i = 0; i < maxAudio; i++)
            {
                var key = $"Audio {i + 1}";
                var col = new DataGridTextColumn { 
                    Header = key, 
                    Binding = new Binding($"[{key}]"), 
                    Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells), 
                    MinWidth = 120, 
                    ElementStyle = cellTextStyle, 
                    CellStyle = cellPaddingStyle 
                };
                FilesDataGrid.Columns.Add(col);
            }

            foreach (var entry in analyzed)
            {
                var fr = new FileRow();
                fr.FileName = entry.Rel;

                var mfi = entry.Mfi;
                for (int vi = 0; vi < maxVideo; vi++)
                {
                    var v = mfi.VideoStreams[vi];
                    fr.Fields[$"Video {vi + 1}"] = v.ToString;
                }

                for (int ai = 0; ai < maxAudio; ai++)
                {
                    var a = mfi.AudioStreams[ai];
                    fr.Fields[$"Audio {ai + 1}"] = a.ToString;
                }

                _rows.Add(fr);
            }
        }
    }

    public class FileRow
    {
        public string FileName { get; set; }
        public Dictionary<string, string> Fields { get; } = new Dictionary<string, string>();
        public string this[string key]
        {
            get
            {
                if (Fields.TryGetValue(key, out var v)) return v;
                return string.Empty;
            }
        }
    }
}
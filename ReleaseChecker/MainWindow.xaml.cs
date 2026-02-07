using MediaInfoLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private static void EnableDarkTitleBar(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            int value = 1;
            DwmSetWindowAttribute(hwnd, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref value, sizeof(int));
        }

        private ObservableCollection<FileRow> _rows = new ObservableCollection<FileRow>();
        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += (_, _) => EnableDarkTitleBar(this);
            FilesDataGrid.ItemsSource = _rows;

            this.MaxWidth = SystemParameters.WorkArea.Width;
            this.MaxHeight = SystemParameters.WorkArea.Height;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            FontSize = Math.Clamp(FontSize + (e.Delta > 0 ? 1 : -1), 8, 40);
            Dispatcher.BeginInvoke(() =>
            {
                foreach (var col in FilesDataGrid.Columns)
                {
                    col.Width = 0;
                    col.Width = DataGridLength.Auto;
                }
            }, System.Windows.Threading.DispatcherPriority.Render);
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

            int maxAudio = analyzed.Max(e => e.Mfi?.AudioStreams?.Count ?? 0);

            AddColumn("Path", "FileName");
            AddColumn("Video", "Video");

            for (int a = 0; a < maxAudio; a++)
                AddColumn(maxAudio == 1 ? "Audio" : $"Audio {a + 1}", $"Audio{a}");

            foreach (var entry in analyzed)
            {
                var fr = new FileRow();
                var mfi = entry.Mfi;

                fr.FileName = entry.Rel;
                fr.Fields["FileName"] = entry.Rel;
                fr.Fields["Video"] = mfi.VideoStream;

                for (int a = 0; a < maxAudio; a++)
                {
                    if (mfi?.AudioStreams != null && a < mfi.AudioStreams.Count)
                        fr.Fields[$"Audio{a}"] = mfi.AudioStreams[a];
                }

                _rows.Add(fr);
            }
        }

        private void AddColumn(string header, string key)
        {
            FilesDataGrid.Columns.Add(new DataGridTemplateColumn
            {
                Header = header,
                CellTemplate = CreateCellTemplate(key)
            });
        }

        private DataTemplate CreateCellTemplate(string key)
        {
            var template = new DataTemplate();
            var factory = new FrameworkElementFactory(typeof(ContentPresenter));
            factory.SetBinding(ContentPresenter.ContentProperty, new Binding($"[{key}]"));
            template.VisualTree = factory;
            return template;
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
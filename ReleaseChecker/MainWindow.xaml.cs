using MediaInfoLib;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
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

            var view = CollectionViewSource.GetDefaultView(_rows);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FileRow.FolderPath)));
            FilesDataGrid.ItemsSource = view;

            this.MaxWidth = SystemParameters.WorkArea.Width;
            this.MaxHeight = SystemParameters.WorkArea.Height;

            var appVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            var miVer = new MediaInfo().Option("Info_Version");
            Title = $"Drop folders or files here [v{appVer?.ToString(3) ?? "?"}] [{miVer}]";
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
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

                var paths = (string[])e.Data.GetData(DataFormats.FileDrop);

                var data = ReadData(paths);
                DumpJson(data);

                foreach (var group in data.GroupBy(e => e.Mfi.FolderPath))
                    Checker.CheckConsistency(group.Select(e => e.Mfi).ToList());

                UpdateUI(data);

                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void DumpJson(List<(string Rel, MediaFileInfo Mfi)> data)
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
            var json = System.Text.Json.JsonSerializer.Serialize(data.Select(d => d.Mfi), options);
            File.WriteAllText("./ReleaseChecker_dump.json", json);
        }

        private void UpdateUI(List<(string Rel, MediaFileInfo Mfi)> analyzed)
        {
            _rows.Clear();
            FilesDataGrid.Columns.Clear();

            int maxAudio = analyzed.Max(e => e.Mfi?.AudioStreams?.Count ?? 0);
            int maxSubs = analyzed.Max(e => e.Mfi?.SubtitleStreams?.Count ?? 0);

            AddColumn("Path", "FileName");
            AddColumn("Video", "Video");

            for (int a = 0; a < maxAudio; a++)
                AddColumn(maxAudio == 1 ? "Audio" : $"Audio {a + 1}", $"Audio{a}");

            for (int s = 0; s < maxSubs; s++)
                AddColumn(maxSubs == 1 ? "Sub" : $"Sub {s + 1}", $"Sub{s}");

            foreach (var entry in analyzed)
            {
                var fr = new FileRow()
                {
                    FileName = entry.Rel,
                    FolderPath = entry.Mfi.FolderPath
                };
                var mfi = entry.Mfi;

                fr.Fields["FileName"] = entry.Rel;
                if (mfi.VideoStream != null) fr.Fields["Video"] = mfi.VideoStream;

                for (int a = 0; a < maxAudio; a++)
                {
                    if (mfi?.AudioStreams != null && a < mfi.AudioStreams.Count)
                        fr.Fields[$"Audio{a}"] = mfi.AudioStreams[a];
                }

                for (int s = 0; s < maxSubs; s++)
                {
                    if (mfi?.SubtitleStreams != null && s < mfi.SubtitleStreams.Count)
                        fr.Fields[$"Sub{s}"] = mfi.SubtitleStreams[s];
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
        public required string FileName { get; set; }
        public required string FolderPath { get; set; }
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
using System.Text;
using System.IO;
using IOPath = System.IO.Path;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var textStyle = CreateTextBlockElementStyle();
            var fileCol = new DataGridTextColumn
            {
                Header = "File",
                Binding = new System.Windows.Data.Binding("FileName"),
                ElementStyle = textStyle,
                Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells),
                MinWidth = 200
            };
            FilesDataGrid.Columns.Add(fileCol);
        }

        private Style CreateTextBlockElementStyle()
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.None));
            return style;
        }

        private static string FlagBox(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "[ ]";
            var v = value.Trim().ToLowerInvariant();
            if (v == "yes" || v == "y" || v == "1" || v == "true" || v == "да" || v == "дa") return "[x]";
            return "[ ]";
        }

        private static string BuildFlagsLangTitle(string def, string forced, string lang, string title, long streamBytes = 0, long fileBytes = 0)
        {
            var flagDef = FlagBox(def);
            var flagForced = FlagBox(forced);
            // combine flags and percent without space between them
            var flagPart = flagDef + flagForced;
            if (fileBytes > 0 && streamBytes > 0)
            {
                try
                {
                    var pct = (int)((streamBytes * 100L) / fileBytes);
                    flagPart = flagPart + $"[{pct}%]";
                }
                catch { }
            }
            var parts = new System.Collections.Generic.List<string> { flagPart };
            var langTitle = string.Join(" ", new[] { lang, title }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(langTitle)) parts.Add(langTitle);
            return string.Join(" ", parts).Trim();
        }

        private static string NormalizeFrameRate(string fr)
        {
            if (string.IsNullOrWhiteSpace(fr)) return string.Empty;
            var s = fr.Trim();
            // Extract first numeric value (like 23.976) and ignore parenthesized ratios
            var m = System.Text.RegularExpressions.Regex.Match(s, "[0-9]+(?:\\.[0-9]+)?");
            if (m.Success)
            {
                if (double.TryParse(m.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                {
                    // format with up to 3 decimal places, trim trailing zeros
                    var outStr = d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                    return outStr + " fps";
                }
            }
            // fallback: return original trimmed string
            return s;
        }

        private static string NormalizeBitrate(string br)
        {
            if (string.IsNullOrWhiteSpace(br)) return string.Empty;
            var s = br.Trim();
            s = s.Replace("Mb/s", "Mbps");
            s = s.Replace("Mb/s", "Mbps");
            s = s.Replace("kb/s", "kbps");
            s = s.Replace("Kb/s", "kbps");
            s = s.Replace("kbit/s", "kbps");
            s = s.Replace("Mb/s", "Mbps");
            return s;
        }

        private static string NormalizeChannels(string ch, string channelPositions)
        {
            if (string.IsNullOrWhiteSpace(ch) && string.IsNullOrWhiteSpace(channelPositions)) return string.Empty;

            // check if channelPositions contains LFE -> then this stream has a .1 LFE channel
            bool hasLfe = false;
            if (!string.IsNullOrWhiteSpace(channelPositions))
            {
                var cp = channelPositions.ToLowerInvariant();
                if (cp.Contains("lfe") || cp.Contains("low frequency")) hasLfe = true;
            }

            var s = (ch ?? string.Empty).Trim().ToLowerInvariant();
            // already like 5.1 or 2.0
            var mDot = System.Text.RegularExpressions.Regex.Match(s, "\\d+\\.\\d+");
            if (mDot.Success) return mDot.Value;

            // common words
            if (s.Contains("stereo")) return "2.0";
            if (s.Contains("mono")) return "1.0";

            // extract number of channels
            var m = System.Text.RegularExpressions.Regex.Match(s, "\\d+");
            if (m.Success)
            {
                if (int.TryParse(m.Value, out var n))
                {
                    if (hasLfe && n > 0)
                    {
                        var main = n - 1;
                        if (main < 1) main = 1;
                        return main + ".1";
                    }
                    else
                    {
                        return n + ".0";
                    }
                }
            }
            return ch ?? string.Empty;
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
            // Use only the last dropped folder (ignore others)
            var lastDir = paths.Reverse().FirstOrDefault(p => Directory.Exists(p));
            _rows.Clear();
            if (lastDir != null)
            {
                try
                {
                    var files = Directory.GetFiles(lastDir, "*", SearchOption.AllDirectories).OrderBy(f => f).ToList();

                    // collect analysis results first
                    var analyzed = new System.Collections.Generic.List<(string Rel, MediaFileInfo Mfi)>();
                    foreach (var file in files)
                    {
                        var rel = IOPath.GetRelativePath(lastDir, file);
                        MediaFileInfo mfi = null;
                        try { mfi = MediaInfoReader.Analyze(file); } catch { mfi = null; }
                        analyzed.Add((rel, mfi));
                    }

                    int maxVideo = analyzed.Max(a => a.Mfi?.VideoStreams?.Count ?? 0);
                    int maxAudio = analyzed.Max(a => a.Mfi?.AudioStreams?.Count ?? 0);

                    // rebuild DataGrid columns: keep File column first
                    FilesDataGrid.Columns.Clear();
                    FilesDataGrid.Columns.Add(new DataGridTextColumn { Header = "File", Binding = new System.Windows.Data.Binding("FileName"), Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells), MinWidth = 200, ElementStyle = CreateTextBlockElementStyle() });

                    for (int i = 0; i < maxVideo; i++)
                    {
                        var key = $"Video {i + 1}";
                        var col = new DataGridTextColumn { Header = key, Binding = new System.Windows.Data.Binding($"[{key}]"), Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells), MinWidth = 120, ElementStyle = CreateTextBlockElementStyle() };
                        FilesDataGrid.Columns.Add(col);
                    }

                    for (int i = 0; i < maxAudio; i++)
                    {
                        var key = $"Audio {i + 1}";
                        var col = new DataGridTextColumn { Header = key, Binding = new System.Windows.Data.Binding($"[{key}]"), Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells), MinWidth = 120, ElementStyle = CreateTextBlockElementStyle() };
                        FilesDataGrid.Columns.Add(col);
                    }

                    // populate rows
                    foreach (var entry in analyzed)
                    {
                        var fr = new FileRow();
                        fr.FileName = entry.Rel;

                        var mfi = entry.Mfi;
                        for (int vi = 0; vi < maxVideo; vi++)
                        {
                            string value = string.Empty;
                            if (mfi != null && mfi.VideoStreams.Count > vi)
                            {
                                var v = mfi.VideoStreams[vi];
                                var flagsLangTitle = BuildFlagsLangTitle(v.Default, v.Forced, v.Language, v.Title, v.StreamSizeBytes, mfi?.FileSizeBytes ?? 0);
                                var bitDepth = string.IsNullOrWhiteSpace(v.BitDepth) ? string.Empty : v.BitDepth.Trim();
                                var formatWithDepth = string.IsNullOrWhiteSpace(bitDepth) ? v.Format : $"{v.Format}@{bitDepth}bit";

                                var techParts = new[] {
                                    formatWithDepth,
                                    string.IsNullOrWhiteSpace(v.Width) || string.IsNullOrWhiteSpace(v.Height) ? string.Empty : $"{v.Width}x{v.Height}",
                                    NormalizeFrameRate(v.FrameRate),
                                    NormalizeBitrate(v.BitRate)
                                }.Where(s => !string.IsNullOrWhiteSpace(s));
                                var tech = string.Join("; ", techParts);
                                value = string.IsNullOrWhiteSpace(tech) ? flagsLangTitle : (flagsLangTitle + "\n" + tech);
                            }
                            fr.Fields[$"Video {vi + 1}"] = value;
                        }

                        for (int ai = 0; ai < maxAudio; ai++)
                        {
                            string value = string.Empty;
                            if (mfi != null && mfi.AudioStreams.Count > ai)
                            {
                                var a = mfi.AudioStreams[ai];
                                var flagsLangTitle = BuildFlagsLangTitle(a.Default, a.Forced, a.Language, a.Title, a.StreamSizeBytes, mfi?.FileSizeBytes ?? 0);
                                var techParts = new[] { a.Format, NormalizeChannels(a.Channels, a.ChannelPositions), a.SamplingRate, NormalizeBitrate(a.BitRate) }.Where(s => !string.IsNullOrWhiteSpace(s));
                                var tech = string.Join("; ", techParts);
                                value = string.IsNullOrWhiteSpace(tech) ? flagsLangTitle : (flagsLangTitle + "\n" + tech);
                            }
                            fr.Fields[$"Audio {ai + 1}"] = value;
                        }

                        _rows.Add(fr);
                    }
                }
                catch
                {
                    // ignore folders we can't access
                }
            }
            e.Handled = true;
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
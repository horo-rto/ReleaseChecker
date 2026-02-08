using MediaInfoLib;
using System.Text.RegularExpressions;
using System.Xml;
using static MediaInfoLib.Options;

namespace ReleaseChecker
{
    /// <summary>
    /// https://github.com/MediaArea/MediaInfoLib/blob/c3f46906117560790247bda52da04e4d8fcef6c7/Source/MediaInfo/MediaInfo_Config_Automatic.cpp
    /// </summary>
    
    public class MediaFileInfo
    {
        public string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public string FolderPath => System.IO.Path.GetDirectoryName(FilePath) ?? String.Empty;
        public long FileSizeBytes { get; set; }

        public VideoStreamInfo? VideoStream;
        public List<AudioStreamInfo> AudioStreams { get; } = new List<AudioStreamInfo>();
        public List<SubtitleStreamInfo> SubtitleStreams { get; } = new List<SubtitleStreamInfo>();

        public MediaFileInfo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            FilePath = filePath;

            var mi = new MediaInfo();

            if (mi.Open(filePath) == 0)
            {
                mi.Close();
                throw new Exception($"MediaInfo failed.");
            }

            FileSizeBytes = MediaInfoReader.SafeGetLong(mi, StreamKind.General, 0, "FileSize");

            if (mi.Count_Get(StreamKind.Video) > 0)
            {
                VideoStream = new VideoStreamInfo(mi, 0, this);
            }

            for (int i = 0; i < mi.Count_Get(StreamKind.Audio); i++)
            {
                AudioStreams.Add(new AudioStreamInfo(mi, i, this));
            }

            for (int i = 0; i < mi.Count_Get(StreamKind.Text); i++)
            {
                SubtitleStreams.Add(new SubtitleStreamInfo(mi, i, this));
            }

            mi.Close();

            CheckLanguageOrder();

        }

        private void CheckLanguageOrder()
        {
            bool foreignSeen = false;
            foreach (var audio in AudioStreams)
            {
                var lang = (audio.Language ?? "").Trim().ToLowerInvariant();
                bool isRussian = lang.Contains("ru") || lang.Contains("рус");

                if (!isRussian && !string.IsNullOrEmpty(lang))
                    foreignSeen = true;

                if (isRussian && foreignSeen)
                    audio.LanguageError = true;
            }
        }
    }

    public class CoreStreamInfo
    {
        public int Index { get; set; }
        public StreamKind StreamKind { get; set; }
        public MediaFileInfo ParentFile { get; set; }
        public long StreamSizeBytes { get; set; }
        public string Format { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public bool? Default { get; set; }
        public bool? Forced { get; set; }

        public CoreStreamInfo(MediaInfo mi, int i, StreamKind kind, MediaFileInfo parent)
        {
            Index = i;
            StreamKind = kind;
            ParentFile = parent;
            StreamSizeBytes = MediaInfoReader.SafeGetLong(mi, kind, i, "StreamSize");
            Format = MediaInfoReader.SafeGet(mi, kind, i, "Format");
            Language = MediaInfoReader.SafeGet(mi, kind, i, "Language/String");
            Title = MediaInfoReader.SafeGet(mi, kind, i, "Title");
            Default = MediaInfoReader.SafeGetTag(mi, kind, i, "Default");
            Forced = MediaInfoReader.SafeGetTag(mi, kind, i, "Forced");
        }

        public string DefaultToString => Default ?? false ? "[x]" : "[ ]";
        public string ForcedToString => Forced ?? false ? "[x]" : "[ ]";
        public string Percentage => ParentFile.FileSizeBytes > 0 ? $"[{(int)(StreamSizeBytes * 100L / ParentFile.FileSizeBytes)}%]" : "[xx%]";
        public bool DefaultError => 
            (Default == true && Index > 0) ||
            (Default == false && Index == 0);
        public bool ForcedError => Forced == true;
        public bool LanguageError { get; set; }
        public bool FormatError { get; set; }
        protected string NormalizeBitrate(string br)
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
    }

    public class VideoStreamInfo : CoreStreamInfo
    {
        public string CodecID { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string BitDepth { get; set; }
        public string FrameRate { get; set; }
        public string BitRate { get; set; }
        public string Duration { get; set; }
        public string AspectRatio { get; set; }

        public VideoStreamInfo(MediaInfo mi, int i, MediaFileInfo parent) : base(mi, i, StreamKind.Video, parent)
        {
            CodecID = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "CodecID");
            Width = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "Width");
            Height = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "Height");
            FrameRate = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "FrameRate/String");
            BitRate = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "BitRate/String");
            Duration = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "Duration/String3");
            AspectRatio = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "DisplayAspectRatio/String");
            BitDepth = string.IsNullOrWhiteSpace(MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "BitDepth")) ?
                MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "Bit depth") :
                MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "BitDepth");
        }

        public string FrameRateToString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FrameRate)) return string.Empty;
                var s = FrameRate.Trim();
                var m = Regex.Match(s, "[0-9]+(?:\\.[0-9]+)?");
                if (m.Success)
                {
                    if (double.TryParse(m.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    {
                        var outStr = d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                        return outStr + " fps";
                    }
                }
                return s;
            }
        }
        public string Resolution => $"{Width}x{Height}";
        public string BitRateToString => NormalizeBitrate(BitRate);
        public string FormatToString => Format switch
        {
            "MPEG-4 Visual" => "XVID",
            _ => Format,
        };

        public bool PercentageError => ParentFile.FileSizeBytes > 0 ? ((StreamSizeBytes * 100L / ParentFile.FileSizeBytes) < 50) : false;
        public bool BitDepthError { get; set; }
        public bool FrameRateError { get; set; }
        public bool ResolutionError { get; set; }

        public new string ToString 
        {
            get
            {
                return $"{DefaultToString}{ForcedToString}{Percentage} {Language} {Title}" + 
                    $"\n{Format}@{BitDepth}bit; {Width}x{Height}; {FrameRateToString}; {BitRateToString}";
            }
        }
    }

    public class AudioStreamInfo : CoreStreamInfo
    {
        public string FormatProfile { get; set; }
        public string AdditionalFeatures { get; set; }
        public string NumberOfDynamicObjects { get; set; }
        public string Channels { get; set; }
        public string ChannelPositions { get; set; }
        public string SamplingRate { get; set; }
        public string BitRate { get; set; }
        public string BitDepth { get; set; }
        public string Duration { get; set; }

        public AudioStreamInfo(MediaInfo mi, int i, MediaFileInfo parent) : base(mi, i, StreamKind.Audio, parent)
        {
            FormatProfile = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Format_Profile");
            AdditionalFeatures = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Format_AdditionalFeatures");
            NumberOfDynamicObjects = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "NumberOfDynamicObjects");
            Channels = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Channel(s)");
            ChannelPositions = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "ChannelLayout");
            SamplingRate = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "SamplingRate/String");
            BitRate = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "BitRate/String");
            BitDepth = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "BitDepth");
            Duration = MediaInfoReader.SafeGet(mi, StreamKind.Audio, i, "Duration/String3");
        }

        public string ChannelsToString
        {
            get
            { 
                if (string.IsNullOrWhiteSpace(Channels)) return string.Empty;

                var s = Channels.ToLowerInvariant();

                if (s.Contains("stereo")) return "2.0";
                if (s.Contains("mono")) return "1.0";

                if (string.IsNullOrWhiteSpace(ChannelPositions)) return Channels + ".0";

                var cp = ChannelPositions.ToLowerInvariant();
                bool hasLfe = cp.Contains("lfe") || cp.Contains("low frequency");

                if (int.TryParse(Channels, out var n))
                {
                    if (hasLfe) return (n - 1) + ".1";
                    else return n + ".0";
                }

                return Channels;
            }
        }
        public string BitRateToString => NormalizeBitrate(BitRate);

        public string FormatRewrittenWithLayer => Format switch
        {
            "MPEG Audio" => "MP" + FormatProfile.Replace("Layer ", ""),
            "AAC" => "AAC" + (string.IsNullOrEmpty(FormatProfile) ? "" : " " + FormatProfile),
            "MLP FBA" => "Dolby TrueHD",
            "DTS XLL" => "DHS-HD MA",
            _ => Format,
        };
        public string FormatToString => AdditionalFeatures switch
        {
            "" => FormatRewrittenWithLayer,
            "XLL" => "DHS-HD MA",
            "JOC" => FormatRewrittenWithLayer + " Atmos",
            _ => NumberOfDynamicObjects switch
            {
                "" => FormatRewrittenWithLayer + " " + AdditionalFeatures,
                _ => FormatRewrittenWithLayer + " Atmos (" + NumberOfDynamicObjects + " objects)",
            },
        };

        public bool PercentageError => ParentFile.VideoStream?.StreamSizeBytes > 0 ? (StreamSizeBytes * 100L / ParentFile.VideoStream.StreamSizeBytes) > 33 : false;
        public bool ChannelsError { get; set; }
        public bool BitRateError { get; set; }

        public new string ToString
        {
            get
            {
                return $"{DefaultToString}{ForcedToString}{Percentage} {Language} {Title}" +
                    $"\n{Format}; {ChannelsToString}; {SamplingRate}; {BitRateToString}";
            }
        }

    }

    public class SubtitleStreamInfo : CoreStreamInfo
    {
        public string LineCount { get; set; }

        public SubtitleStreamInfo(MediaInfo mi, int i, MediaFileInfo parent) : base(mi, i, StreamKind.Text, parent)
        {
            LineCount = MediaInfoReader.SafeGet(mi, StreamKind.Text, i, "ElementCount");
        }
        public string FormatToString => Format switch
        {
            "UTF-8" => "SRT",
            _ => Format,
        };
        public new bool DefaultError =>
            (Default == true && Index > 0);
    }

    public static class MediaInfoReader
    {
        internal static string SafeGet(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
        {
            try
            {
                var v = mi.Get(kind, streamNumber, parameter);
                return string.IsNullOrEmpty(v) ? string.Empty : v;
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static long SafeGetLong(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
        {
            try
            {
                var v = mi.Get(kind, streamNumber, parameter);
                if (string.IsNullOrWhiteSpace(v)) return 0;
                // try parse digits
                if (long.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var l)) return l;
                var m = System.Text.RegularExpressions.Regex.Match(v, "[0-9]+(?:\\.[0-9]+)?");
                if (m.Success)
                {
                    if (long.TryParse(m.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var l2)) return l2;
                    if (double.TryParse(m.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d)) return (long)d;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        internal static bool? SafeGetTag(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
        {
            try
            {
                var value = mi.Get(kind, streamNumber, parameter);

                if (string.IsNullOrWhiteSpace(value)) return false;

                var v = value.Trim().ToLowerInvariant();
                if (v == "yes" || v == "y" || v == "1" || v == "true" || v == "��") return true;

                return false;
            }
            catch
            {
                return null;
            }
        }
    }
}

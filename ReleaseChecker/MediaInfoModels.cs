using System;
using System.Collections.Generic;
using System.Linq;
using MediaInfoLib;

namespace ReleaseChecker
{
    public class MediaFileInfo
    {
        public string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public string FileSize { get; set; }
        public long FileSizeBytes { get; set; }
        public string Duration { get; set; }

        public List<VideoStreamInfo> VideoStreams { get; } = new List<VideoStreamInfo>();
        public List<AudioStreamInfo> AudioStreams { get; } = new List<AudioStreamInfo>();
        public List<SubtitleStreamInfo> SubtitleStreams { get; } = new List<SubtitleStreamInfo>();
    }

    public class VideoStreamInfo
    {
        public int Index { get; set; }
        public string Format { get; set; }
        public string CodecID { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string BitDepth { get; set; }
        public long StreamSizeBytes { get; set; }
        public string FrameRate { get; set; }
        public string BitRate { get; set; }
        public string Duration { get; set; }
        public string AspectRatio { get; set; }
        public string PixelFormat { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public string Default { get; set; }
        public string Forced { get; set; }
    }

    public class AudioStreamInfo
    {
        public int Index { get; set; }
        public string Format { get; set; }
        public string CodecID { get; set; }
        public string Channels { get; set; }
        public string ChannelPositions { get; set; }
        public string SamplingRate { get; set; }
        public string BitRate { get; set; }
        public string Language { get; set; }
        public string Duration { get; set; }
        public string Title { get; set; }
        public string Default { get; set; }
        public string Forced { get; set; }
        public long StreamSizeBytes { get; set; }
    }

    public class SubtitleStreamInfo
    {
        public int Index { get; set; }
        public string Format { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public string Default { get; set; }
        public string Forced { get; set; }
        public long StreamSizeBytes { get; set; }
    }

    public static class MediaInfoReader
    {
        public static MediaFileInfo Analyze(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));

            var info = new MediaFileInfo { FilePath = filePath };

            var mi = new MediaInfo();
            try
            {
                var openResult = mi.Open(filePath);
                if (openResult == 0)
                {
                    // unable to open
                    return info;
                }

                // General
                info.FileSize = SafeGet(mi, StreamKind.General, 0, "FileSize/String");
                info.FileSizeBytes = SafeGetLong(mi, StreamKind.General, 0, "FileSize");
                info.Duration = SafeGet(mi, StreamKind.General, 0, "Duration/String3");

                // Video streams
                int videoCount = mi.Count_Get(StreamKind.Video);
                for (int i = 0; i < videoCount; i++)
                {
                    var vs = new VideoStreamInfo
                    {
                        Index = i,
                        Format = SafeGet(mi, StreamKind.Video, i, "Format"),
                        CodecID = SafeGet(mi, StreamKind.Video, i, "CodecID"),
                        Width = SafeGet(mi, StreamKind.Video, i, "Width"),
                        Height = SafeGet(mi, StreamKind.Video, i, "Height"),
                        FrameRate = SafeGet(mi, StreamKind.Video, i, "FrameRate/String"),
                        BitRate = SafeGet(mi, StreamKind.Video, i, "BitRate/String"),
                        Duration = SafeGet(mi, StreamKind.Video, i, "Duration/String3"),
                        AspectRatio = SafeGet(mi, StreamKind.Video, i, "DisplayAspectRatio/String"),
                        PixelFormat = SafeGet(mi, StreamKind.Video, i, "ChromaSubsampling"),
                        BitDepth = string.IsNullOrWhiteSpace(SafeGet(mi, StreamKind.Video, i, "BitDepth")) ? SafeGet(mi, StreamKind.Video, i, "Bit depth") : SafeGet(mi, StreamKind.Video, i, "BitDepth"),
                        StreamSizeBytes = SafeGetLong(mi, StreamKind.Video, i, "StreamSize"),
                        Language = SafeGet(mi, StreamKind.Video, i, "Language/String"),
                        Title = SafeGet(mi, StreamKind.Video, i, "Title"),
                        Default = SafeGet(mi, StreamKind.Video, i, "Default"),
                        Forced = SafeGet(mi, StreamKind.Video, i, "Forced")
                    };
                    info.VideoStreams.Add(vs);
                }

                // Audio streams
                int audioCount = mi.Count_Get(StreamKind.Audio);
                for (int i = 0; i < audioCount; i++)
                {
                    var a = new AudioStreamInfo
                    {
                        Index = i,
                        Format = SafeGet(mi, StreamKind.Audio, i, "Format"),
                        CodecID = SafeGet(mi, StreamKind.Audio, i, "CodecID"),
                        Channels = SafeGet(mi, StreamKind.Audio, i, "Channel(s)"),
                        ChannelPositions = SafeGet(mi, StreamKind.Audio, i, "ChannelPositions/String"),
                        SamplingRate = SafeGet(mi, StreamKind.Audio, i, "SamplingRate/String"),
                        BitRate = SafeGet(mi, StreamKind.Audio, i, "BitRate/String"),
                        Language = SafeGet(mi, StreamKind.Audio, i, "Language/String"),
                        Duration = SafeGet(mi, StreamKind.Audio, i, "Duration/String3"),
                        Title = SafeGet(mi, StreamKind.Audio, i, "Title"),
                        Default = SafeGet(mi, StreamKind.Audio, i, "Default"),
                        Forced = SafeGet(mi, StreamKind.Audio, i, "Forced"),
                        StreamSizeBytes = SafeGetLong(mi, StreamKind.Audio, i, "StreamSize")
                    };
                    info.AudioStreams.Add(a);
                }

                // Text (subtitles) streams
                int textCount = mi.Count_Get(StreamKind.Text);
                for (int i = 0; i < textCount; i++)
                {
                    var s = new SubtitleStreamInfo
                    {
                        Index = i,
                        Format = SafeGet(mi, StreamKind.Text, i, "Format"),
                        Language = SafeGet(mi, StreamKind.Text, i, "Language/String"),
                        Title = SafeGet(mi, StreamKind.Text, i, "Title"),
                        Default = SafeGet(mi, StreamKind.Text, i, "Default"),
                        Forced = SafeGet(mi, StreamKind.Text, i, "Forced"),
                        StreamSizeBytes = SafeGetLong(mi, StreamKind.Text, i, "StreamSize")
                    };
                    info.SubtitleStreams.Add(s);
                }
            }
            finally
            {
                try { mi.Close(); } catch { }
            }

            return info;
        }

        private static string SafeGet(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
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

        private static long SafeGetLong(MediaInfo mi, StreamKind kind, int streamNumber, string parameter)
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
    }
}

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
        public string FrameRate { get; set; }
        public string BitRate { get; set; }
        public string Duration { get; set; }
        public string AspectRatio { get; set; }
        public string PixelFormat { get; set; }
    }

    public class AudioStreamInfo
    {
        public int Index { get; set; }
        public string Format { get; set; }
        public string CodecID { get; set; }
        public string Channels { get; set; }
        public string SamplingRate { get; set; }
        public string BitRate { get; set; }
        public string Language { get; set; }
        public string Duration { get; set; }
    }

    public class SubtitleStreamInfo
    {
        public int Index { get; set; }
        public string Format { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
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
                        PixelFormat = SafeGet(mi, StreamKind.Video, i, "ChromaSubsampling")
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
                        SamplingRate = SafeGet(mi, StreamKind.Audio, i, "SamplingRate/String"),
                        BitRate = SafeGet(mi, StreamKind.Audio, i, "BitRate/String"),
                        Language = SafeGet(mi, StreamKind.Audio, i, "Language/String"),
                        Duration = SafeGet(mi, StreamKind.Audio, i, "Duration/String3")
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
                        Title = SafeGet(mi, StreamKind.Text, i, "Title")
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
    }
}

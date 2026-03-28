using MediaInfoLib;

namespace ReleaseChecker
{
    /// <summary>
    /// https://github.com/MediaArea/MediaInfoLib/blob/c3f46906117560790247bda52da04e4d8fcef6c7/Source/MediaInfo/MediaInfo_Config_Automatic.cpp
    /// </summary>
    public class MediaFileInfo
    {
        public string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
        public string FolderPath => System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty;
        public long FileSizeBytes { get; set; }
        public IntegrityLevel IntegrityLevel { get; private set; } = IntegrityLevel.Ok;
        public string IntegrityText { get; private set; } = string.Empty;

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
                throw new Exception("MediaInfo failed.");
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

            AnalyzeIntegrity(mi);

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
                    audio.LanguageOrderError = true;
            }
        }

        private void AnalyzeIntegrity(MediaInfo mi)
        {
            string conformanceErrors = MediaInfoReader.SafeGet(mi, StreamKind.General, 0, "ConformanceErrors");
            string conformanceWarnings = MediaInfoReader.SafeGet(mi, StreamKind.General, 0, "ConformanceWarnings");

            bool hasConformanceErrors = !MediaInfoReader.IsZeroOrEmpty(conformanceErrors);
            bool hasConformanceWarnings = !MediaInfoReader.IsZeroOrEmpty(conformanceWarnings);
            bool truncated = MediaInfoReader.SafeGetTag(mi, StreamKind.General, 0, "IsTruncated") == true;

            if (truncated || hasConformanceErrors)
            {
                IntegrityLevel = IntegrityLevel.Fail;
                IntegrityText = $"ERR | {(truncated ? "TRUNCATED | " : "")}Errors: {conformanceErrors}";
                return;
            }

            if (hasConformanceWarnings)
            {
                IntegrityLevel = IntegrityLevel.Warn;
                IntegrityText = $"WARN | Warnings: {hasConformanceWarnings}";
                return;
            }
        }
    }

    public enum IntegrityLevel
    {
        Ok = 0,
        Warn = 1,
        Fail = 2
    }
}
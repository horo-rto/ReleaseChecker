using MediaInfoLib;

namespace ReleaseChecker
{
    public class CoreStreamInfo
    {
        public int Index { get; set; }
        public StreamKind StreamKind { get; set; }
        public MediaFileInfo ParentFile { get; set; }
        public long StreamSizeBytes { get; set; }
        public string BitRate { get; set; }
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
            BitRate = MediaInfoReader.SafeGet(mi, kind, i, "BitRate/String");
            Format = MediaInfoReader.SafeGet(mi, kind, i, "Format");
            Language = MediaInfoReader.SafeGet(mi, kind, i, "Language/String");
            Title = MediaInfoReader.SafeGet(mi, kind, i, "Title");
            Default = MediaInfoReader.SafeGetTag(mi, kind, i, "Default");
            Forced = MediaInfoReader.SafeGetTag(mi, kind, i, "Forced");
        }

        public string BitRateToString => NormalizeBitrate(BitRate);
        public string DefaultToString => Default ?? false ? "[x]" : "[ ]";
        public string ForcedToString => Forced ?? false ? "[x]" : "[ ]";
        public string Percentage => ParentFile.FileSizeBytes > 0 ? $"[{(int)(StreamSizeBytes * 100L / ParentFile.FileSizeBytes)}%]" : "[xx%]";
        public bool DefaultError =>
            (Default == true && Index > 0) ||
            (Default == false && Index == 0);
        public bool ForcedError => Forced == true;
        public bool LanguageError { get; set; }
        public bool TitleError { get; set; }
        public bool FormatError { get; set; }
        public int BitRateError { get; set; }

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
}
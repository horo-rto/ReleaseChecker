using MediaInfoLib;

namespace ReleaseChecker
{
    public class SubtitleStreamInfo : CoreStreamInfo
    {
        public long LineCount { get; set; } = -1;

        public SubtitleStreamInfo(MediaInfo mi, int i, MediaFileInfo parent) : base(mi, i, StreamKind.Text, parent)
        {
            if (mi.Get(StreamKind.Text, i, "ElementCount") != "")
                LineCount = MediaInfoReader.SafeGetLong(mi, StreamKind.Text, i, "ElementCount");
        }

        public bool IsRussianSignsByTitle => Language.Contains("Russian") && (Title.Contains("Sign") || Title.Contains("Надписи"));

        public string FormatToString => Format switch
        {
            "UTF-8" => "SRT",
            _ => Format,
        };

        public new bool DefaultError =>
            (Default == true && Index > 0);

        public bool SignsError { get; set; }
        public bool LineCountWarning { get; set; }
    }
}
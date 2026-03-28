using MediaInfoLib;

namespace ReleaseChecker
{
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
}
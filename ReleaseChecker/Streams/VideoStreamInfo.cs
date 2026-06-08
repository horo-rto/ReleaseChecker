using MediaInfoLib;
using System.Text.RegularExpressions;

namespace ReleaseChecker
{
    public class VideoStreamInfo : CoreStreamInfo
    {
        public string CodecID { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string BitDepth { get; set; }
        public string FrameRate { get; set; }
        public string FrameRateMode { get; set; }
        public string AspectRatio { get; set; }

        public VideoStreamInfo(MediaInfo mi, int i, MediaFileInfo parent) : base(mi, i, StreamKind.Video, parent)
        {
            CodecID = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "CodecID");
            Width = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "Width");
            Height = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "Height");
            FrameRate = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "FrameRate/String");
            FrameRateMode = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "FrameRate_Mode");
            AspectRatio = MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "DisplayAspectRatio/String");
            BitDepth = string.IsNullOrWhiteSpace(MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "BitDepth"))
                ? MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "Bit depth")
                : MediaInfoReader.SafeGet(mi, StreamKind.Video, i, "BitDepth");
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
        public string ResolutionWithAspectRatio => string.IsNullOrWhiteSpace(AspectRatio)
            ? Resolution
            : $"{Resolution} ({AspectRatio})";
        public string FormatToString => Format switch
        {
            "MPEG-4 Visual" => "XVID",
            _ => Format,
        };

        public bool PercentageError => ParentFile.FileSizeBytes > 0 ? (StreamSizeBytes * 100f / ParentFile.FileSizeBytes) < 50 : false;
        public bool BitDepthError { get; set; }
        public bool FrameRateError { get; set; }
        public bool ResolutionError { get; set; }
        public bool AspectRatioError { get; set; }
        public bool ResolutionOrAspectRatioError => ResolutionError || AspectRatioError;

        public new string ToString
        {
            get
            {
                return $"{DefaultToString}{ForcedToString}{Percentage} {Language} {Title}" +
                    $"\n{Format}@{BitDepth}bit; {Width}x{Height}; {FrameRateToString}; {BitRateToString}";
            }
        }
    }
}
using System.Text;
using ErsatzTV.Core.Interfaces.FFmpeg;

namespace ErsatzTV.Core.FFmpeg;

public class SubtitleBuilder
{
    private readonly ITempFilePool _tempFilePool;
    private Option<int> _alignment;
    private Option<int> _borderStyle;
    private string _content;
    private Option<TimeSpan> _end;
    private Option<string> _fontName;
    private Option<int> _fontSize;
    private int _marginLeft;
    private int _marginRight;
    private int _marginV;
    private Option<string> _outlineColor;
    private Option<string> _primaryColor;
    private Option<IDisplaySize> _resolution = None;
    private Option<int> _shadow;
    private Option<TimeSpan> _start;

    public SubtitleBuilder(ITempFilePool tempFilePool) => _tempFilePool = tempFilePool;

    public SubtitleBuilder WithResolution(IDisplaySize resolution)
    {
        _resolution = Some(resolution);
        return this;
    }

    public SubtitleBuilder WithFontName(string fontName)
    {
        _fontName = fontName;
        return this;
    }

    public SubtitleBuilder WithFontSize(int fontSize)
    {
        _fontSize = fontSize;
        return this;
    }

    public SubtitleBuilder WithPrimaryColor(string primaryColor)
    {
        _primaryColor = primaryColor;
        return this;
    }

    public SubtitleBuilder WithOutlineColor(string outlineColor)
    {
        _outlineColor = outlineColor;
        return this;
    }

    public SubtitleBuilder WithAlignment(int alignment)
    {
        _alignment = alignment;
        return this;
    }

    public SubtitleBuilder WithMarginRight(int marginRight)
    {
        _marginRight = marginRight;
        return this;
    }

    public SubtitleBuilder WithMarginLeft(int marginLeft)
    {
        _marginLeft = marginLeft;
        return this;
    }

    public SubtitleBuilder WithMarginV(int marginV)
    {
        _marginV = marginV;
        return this;
    }

    public SubtitleBuilder WithBorderStyle(int borderStyle)
    {
        _borderStyle = borderStyle;
        return this;
    }

    public SubtitleBuilder WithShadow(int shadow)
    {
        _shadow = shadow;
        return this;
    }

    public SubtitleBuilder WithFormattedContent(string content)
    {
        _content = content;
        return this;
    }

    public SubtitleBuilder WithStartEnd(TimeSpan start, TimeSpan end)
    {
        _start = start;
        _end = end;
        return this;
    }

    public async Task<string> BuildFile()
    {
        string fileName = _tempFilePool.GetNextTempFile(TempFileCategory.Subtitle);

        var sb = new StringBuilder();
        sb.AppendLine("[Script Info]");
        sb.AppendLine("ScriptType: v4.00+");
        sb.AppendLine("WrapStyle: 0");
        sb.AppendLine("ScaledBorderAndShadow: yes");
        sb.AppendLine("YCbCr Matrix: None");

        foreach (IDisplaySize resolution in _resolution)
        {
            sb.AppendLine($"PlayResX: {resolution.Width}");
            sb.AppendLine($"PlayResY: {resolution.Height}");
        }

        sb.AppendLine("[V4+ Styles]");
        sb.AppendLine(
            "Format: Name, Fontname, Fontsize, PrimaryColour, OutlineColour, BorderStyle, Outline, Shadow, Alignment, Encoding");
        sb.AppendLine(
            $"Style: Default,{await _fontName.IfNoneAsync("")},{await _fontSize.IfNoneAsync(32)},{await _primaryColor.IfNoneAsync("")},{await _outlineColor.IfNoneAsync("")},{await _borderStyle.IfNoneAsync(0)},1,{await _shadow.IfNoneAsync(0)},{await _alignment.IfNoneAsync(0)},1");

        var start = "0:00:00.00";
        foreach (TimeSpan startTime in _start)
        {
            start = $"{(int)startTime.TotalHours:00}:{startTime.ToString(@"mm\:ss\.ff")}";
        }

        var end = "99:99:99.99";
        foreach (TimeSpan endTime in _end)
        {
            end = $"{(int)endTime.TotalHours:00}:{endTime.ToString(@"mm\:ss\.ff")}";
        }

        sb.AppendLine("[Events]");
        sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");
        sb.AppendLine(
            @$"Dialogue: 0,{start},{end},Default,,{_marginLeft},{_marginRight},{_marginV},,{{\fad(1200,1200)}}{_content}");

        await File.WriteAllTextAsync(fileName, sb.ToString());

        return fileName;
    }
}

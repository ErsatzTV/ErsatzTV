using System.Text.RegularExpressions;
using System.Xml;

namespace ErsatzTV.Scanner.Core.Metadata.Nfo;

public abstract partial class NfoReaderBase
{
    protected static readonly byte[] Buffer = new byte[8 * 1024 * 1024];
    protected static readonly Regex Pattern = ControlCharacters();

    protected static readonly XmlReaderSettings Settings =
        new()
        {
            Async = true,
            ConformanceLevel = ConformanceLevel.Fragment,
            ValidationType = ValidationType.None,
            CheckCharacters = false,
            IgnoreProcessingInstructions = true,
            IgnoreComments = true
        };

    [GeneratedRegex("[\\p{C}-[\\r\\n\\t]]+")]
    private static partial Regex ControlCharacters();
}

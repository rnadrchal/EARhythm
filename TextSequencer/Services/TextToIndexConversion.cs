using System.Collections.Generic;

namespace TextSequencer.Services;

public static class TextToIndexConversion
{
    public static IEnumerable<int> Convert(string text, TextToIndicesConversion conversion)
    {
        return conversion switch
        {
            TextToIndicesConversion.Ascii => new AsciiToIndices().Convert(text),
            _ => throw new System.NotImplementedException(),
        };
    }
}
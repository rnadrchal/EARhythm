using System.Collections.Generic;
using System.Linq;

namespace TextSequencer.Services;

public class AsciiToIndices : ITextToIndices
{
    public IEnumerable<int> Convert(string text)
    {
        return text.Select(c => (int)c);
    }
}
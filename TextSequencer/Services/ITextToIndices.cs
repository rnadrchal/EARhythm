using System.Collections.Generic;

namespace TextSequencer.Services;

public interface ITextToIndices
{
    IEnumerable<int> Convert(string text);
}
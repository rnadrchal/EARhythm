using Egami.Sequencer;
using TextSequencer.ViewModels;

namespace TextSequencer.Services.SequenceGeneration;

public interface IMusicalSequenceFromChars
{
    MusicalSequence Generate(ICharacterArray characterArray);
}
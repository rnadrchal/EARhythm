using Egami.Sequencer;
using Melanchall.DryWetMidi.Common;
using TextSequencer.ViewModels;

namespace TextSequencer.Services.SequenceGeneration;

public sealed class HarmonyFromChars : IMusicalSequenceFromChars
{
    public MusicalSequence Generate(ICharacterArray characterArray)
    {
        var steps = new SequenceStep[characterArray.IndicesPc.Length];
        var index = 0;
        for (var i = 0; i < steps.Length; i++)
        {
            var length = 16 * characterArray.IndicesAlpha[i];
            
            steps[i] = characterArray.IndicesPc[i] >= 0
                ? new SequenceStep(index, length, (SevenBitNumber)(characterArray.IndicesPc[i] + 60), characterArray.Characters[i].CharToVelocity())
                : new SequenceStep(i * 16, 16, (SevenBitNumber)0, (SevenBitNumber)0);

            index += length;
        }

        return new MusicalSequence(steps);
    }
}
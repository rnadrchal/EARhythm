using System;
using Egami.Sequencer;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using TextSequencer.ViewModels;

namespace TextSequencer.Services.SequenceGeneration;

public class OstinatoBassFromChars : IMusicalSequenceFromChars
{
    public MusicalSequence Generate(ICharacterArray characterArray)
    {
        var noteName = Enum.Parse<NoteName>(characterArray.MedianAlphaIndexNoteName.Replace("#", "Sharp")) ;
        var pitch = (int)noteName +  24;
        var length = characterArray.SumAlpha * 16;
        var velocity = characterArray.MedianAlphaIndexChar.CharToVelocity();
        var step = new SequenceStep(0, length, (SevenBitNumber)pitch, velocity);
        return new MusicalSequence(new[] { step });
    }
}
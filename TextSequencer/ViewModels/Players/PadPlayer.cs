using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TextSequencer.Services.SequenceGeneration;

namespace TextSequencer.ViewModels.Players;

public class PadPlayer : PlayerBase
{
    private readonly IMusicalSequenceFromChars _padPlayerSequenceGenerator = new HarmonyFromChars();
    private readonly GridSequencePlayer _padPlayer;

    private MusicalSequence _padSequence = MusicalSequence.Empty;

    public override string Title => "Pad - Harmony";

    public PadPlayer(GridDivision division, FourBitNumber channel) : base(division, channel)
    {
        _padPlayer = new GridSequencePlayer(
            _padSequence,
            MidiDevices.Output,
            channel,
            NoteClock);
    }

    public string PitchClassSet => string.Join(", ", CharacterArray.NoteNames);

    public string Lengths => string.Join(", ", CharacterArray.IndicesAlpha);

    public override void SetCharacterArray(ICharacterArray characterArray)
    {
        base.SetCharacterArray(characterArray);

        UpdateSequence(CharacterArray);
    }

    private void UpdateSequence(ICharacterArray characterArray)
    {
        _padSequence = _padPlayerSequenceGenerator.Generate(characterArray);
        _padPlayer.SetSequence(_padSequence);

        RaisePropertyChanged(nameof(PitchClassSet));
        RaisePropertyChanged(nameof(Lengths));
    }

    protected override void OnMidiClock(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _padPlayer.Start();
        }
        if (e.Event is StopEvent)
        {
            _padPlayer.Stop();
        }

        base.OnMidiClock(sender, e);
    }
}
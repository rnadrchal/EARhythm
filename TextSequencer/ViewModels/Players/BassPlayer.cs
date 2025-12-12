using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TextSequencer.Services.SequenceGeneration;

namespace TextSequencer.ViewModels.Players;

public sealed class BassPlayer : PlayerBase
{
    private readonly IMusicalSequenceFromChars _bassSequenceGenerator = new OstinatoBassFromChars();
    private readonly GridSequencePlayer _bassPlayer;

    private MusicalSequence _bassSequence = MusicalSequence.Empty;

    public BassPlayer(GridDivision division, FourBitNumber channel) : base(division, channel)
    {
        _bassPlayer = new GridSequencePlayer(
            _bassSequence,
            MidiDevices.Output,
            channel,
            NoteClock); ;
    }

    public override void SetCharacterArray(ICharacterArray characterArray)
    {
        _bassSequence = _bassSequenceGenerator.Generate(characterArray);
        _bassPlayer.SetSequence(_bassSequence, resetPosition: true);
    }

    protected override void OnMidiClock(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is TimingClockEvent)
        {
            //_bassLfoClock.OnClockPulse();
        }

        if (e.Event is StartEvent)
        {
            _bassPlayer.Start();
            //_bassLfoPlayer.Start();
        }
        if (e.Event is StopEvent)
        {
            _bassPlayer.Stop();
            //_bassLfoPlayer.Stop();
        }

        base.OnMidiClock(sender, e);
    }
}
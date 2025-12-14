using Egami.Rhythm.Midi;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;

namespace TextSequencer.ViewModels.Players;

public abstract class PlayerBase : BindableBase
{
    private readonly FourBitNumber _channel;
    protected FourBitNumber Channel => _channel;

    private readonly MidiClockGrid _noteClock;
    protected MidiClockGrid NoteClock => _noteClock;

    private CharacterArray _characterArray = new CharacterArray();
    protected CharacterArray CharacterArray
    {
        get => _characterArray;
        set
        {
            if (SetProperty(ref _characterArray, value))
            {

            }
        }
    }

    public abstract string Title { get; }

    protected PlayerBase(GridDivision division, FourBitNumber channel)
    {
        _channel = channel;
        _noteClock = new MidiClockGrid(division);
        MidiDevices.Input.EventReceived += OnMidiClock;
    }

    protected virtual void OnMidiClock(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is TimingClockEvent)
        {
            _noteClock.OnClockPulse();
        }
    }

    public virtual void SetCharacterArray(ICharacterArray characterArray)
    {
        CharacterArray = (CharacterArray)characterArray;
    }
}
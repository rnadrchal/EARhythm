using System;
using System.Linq;
using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Extensions;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using TextSequencer.Services;

namespace TextSequencer.ViewModels.Players;

public class ArpeggioPlayer : PlayerBase
{
    private readonly GridSequencePlayer _arpPlayer;
    private MusicalSequence _arpSequence = MusicalSequence.Empty;

    private int _octave = 6;
    public int Octave
    {
        get => _octave;
        set
        {
            if (SetProperty(ref _octave, value))
            {
                UpdateSequence(CharacterArray);
            }
        }
    }

    private bool _invert;
    public bool Invert
    {
        get => _invert;
        set
        {
            if (SetProperty(ref _invert, value))
            {
                UpdateSequence(CharacterArray);
            }
        }
    }

    private bool _retrograde;
    public bool Retrograde
    {
        get => _retrograde;
        set
        {
            if (SetProperty(ref _retrograde, value))
            {
                UpdateSequence(CharacterArray);
            }
        }
    }

    public GridDivision[] AvailableDivisions => Enum.GetValues<GridDivision>().Where(d => d < GridDivision.Whole).ToArray();

    private GridDivision _division;

    public GridDivision Division
    {
        get => _division;
        set
        {
            if (SetProperty(ref _division, value))
            {
                NoteClock.SetDivision(_division);
                UpdateSequence(CharacterArray);
            }
        }
    }


    public ArpeggioPlayer(GridDivision division, FourBitNumber channel) : base(division, channel)
    {
        _division = division;
        _arpPlayer = new GridSequencePlayer(
            _arpSequence,
            MidiDevices.Output,
            channel,
            NoteClock);
    }


    public override string Title => "Arpeggio";

    public override void SetCharacterArray(ICharacterArray characterArray)
    {
        base.SetCharacterArray(characterArray);

        UpdateSequence(characterArray);
    }

    protected override void OnMidiClock(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _arpPlayer.Start();
        }
        if (e.Event is StopEvent)
        {
            _arpPlayer.Stop();
        }
        base.OnMidiClock(sender, e);
    }

    private void UpdateSequence(ICharacterArray characterArray)
    {
        var sequence = characterArray.IndicesPc
            .Select(i => i + _octave * 12)
            .Select((n, i) => new SequenceStep(i, 1, (SevenBitNumber)n, characterArray.Characters[i].CharToVelocity()))
            .ToArray();


        _arpSequence = new MusicalSequence(sequence);
        if (_invert)
        {
            var axisPitch = characterArray.IndicesPc.First() + _octave * 12;
            _arpSequence = _arpSequence.Invert(axisPitch);
        }

        if (_retrograde)
        {
            _arpSequence = _arpSequence.Retrograde();
        }
        _arpPlayer.SetSequence(_arpSequence);

    }
}
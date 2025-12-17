using System;
using System.Collections.Generic;
using System.Linq;
using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using TextSequencer.Services;

namespace TextSequencer.ViewModels.Players;

public class PercussionPlayer : PlayerBase
{
    public MeanType[] MeanTypes => Enum.GetValues<MeanType>();
    private readonly GridSequencePlayer _percussionPlayer;
    private MusicalSequence _percussionSequence = MusicalSequence.Empty;

    private MeanType _meanType = MeanType.Harmonic;
    public MeanType MeanType
    {
        get => _meanType;
        set
        {
            if (SetProperty(ref _meanType, value))
            {
                UpdateSequence(CharacterArray);
            }
        }
    }

    public GridDivision[] AvailableDivisions => Enum.GetValues<GridDivision>().Where(d => d >= GridDivision.Sixteenth && d <= GridDivision.Whole).ToArray();

    private GridDivision _division;
    public GridDivision Division
    {
        get => _division;
        set
        {
            if (SetProperty(ref _division, value))
            {
                NoteClock.SetDivision(value);
            }
        }
    }

    public double MeanPc => CharacterArray.MeanPc;

    public PercussionPlayer(GridDivision division, FourBitNumber channel) : base(division, channel)
    {
        _division = division;
        _percussionPlayer = new GridSequencePlayer(
            _percussionSequence,
            MidiDevices.Output,
            channel,
            NoteClock);
    }

    public override string Title => "Rhythm";

    public override void SetCharacterArray(ICharacterArray characterArray)
    {
        base.SetCharacterArray(characterArray);
        UpdateSequence(characterArray);
    }

    protected override void OnMidiClock(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _percussionPlayer.Start();
        }
        if (e.Event is StopEvent)
        {
            _percussionPlayer.Stop();
        }
        base.OnMidiClock(sender, e);
    }

    private void UpdateSequence(ICharacterArray characterArray)
    {
        var cutoff = characterArray.MeanPc;

        var steps = new List<SequenceStep>();
        for (var i = 0; i < characterArray.IndicesPc.Length; i++)
        {
            var pc = characterArray.IndicesPc[i];
            var pitch = (SevenBitNumber)(byte)(pc < cutoff
                ? GeneralMidiPercussion.BassDrum1
                : GeneralMidiPercussion.ClosedHiHat);
            var velocity = pc < 0 ? (SevenBitNumber)0 : characterArray.Characters[i].CharToVelocity();

            var charIndexMapped = characterArray.IndicesAlpha[i] >= 0
                ? NumericUtils.Map(
                    characterArray.IndicesAlpha[i],
                    0,
                    'z' + 7 - 'A',
                    0,
                    127,
                    clamp: true)
                : 0.0;

            var ccs = new Dictionary<int, SevenBitNumber>()
            {
                {
                    1, (SevenBitNumber)(Math.Clamp(pc * 127.0 / 11.0, 0, 127))
                },
                {
                    2, (SevenBitNumber)charIndexMapped
                }
            };

            steps.Add(new SequenceStep(
                i, 1,
                pitch,
                velocity,
                ccValues: ccs));
        }

        _percussionSequence = new MusicalSequence(steps);
        _percussionPlayer.SetSequence(_percussionSequence);

        RaisePropertyChanged(nameof(MeanPc));
    }
}
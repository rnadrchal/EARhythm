using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Windows;
using Egami.Chemistry.Graph;
using Egami.Chemistry.Model;
using Egami.Chemistry.Sequencer;
using Egami.Rhythm.Midi;
using Egami.Sequencer;
using Egami.Sequencer.Grid;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;

namespace ChemSequencer.ViewModels;

public class MoleculePlayer : BindableBase
{
    private readonly PlayerSettings _settings;
    private readonly MidiClockGrid _clock;
    private readonly GridSequencePlayerV2 _player;
    private MusicalSequence _sequence;
    private long _tickCount = 0;
    private MoleculeModel _model;
    private readonly MoleculePlaybackViewModel _moleculePlayback;

    private readonly MoleculeSequenceBuildOptions _buildOptions;
    private readonly TraversalOptions _traversalOptions;
    private readonly PacketSettings _packetSettings;

    public MoleculePlaybackViewModel MoleculePlayback => _moleculePlayback;

    internal MoleculeModel Model => _model;

    public PlayerSettings Settings => _settings;

    public bool HasSequence => _sequence != null;

    private bool _ledOn;

    public bool LedOn
    {
        get => _ledOn;
        set => SetProperty(ref _ledOn, value);
    }

    private List<StepLed> _stepLeds = new List<StepLed>();
    public List<StepLed> StepLeds
    {
        get => _stepLeds;
        set => SetProperty(ref _stepLeds, value);
    }

    public MoleculePlayer()
    {
        _packetSettings = new PacketSettings();
        _traversalOptions = new TraversalOptions(
            MaxRevisitsPerNode: 2,
            BacktrackPolicy: BacktracePolicy.OnlyInCycles);
        _buildOptions = new MoleculeSequenceBuildOptions(
            Mode: TraversalMode.DfsEdgeVisited,
            TraversalOptions: _traversalOptions);
        _clock = new MidiClockGrid(PlayerSettings.DefaultDivision);
        _settings = new PlayerSettings(_clock, this, _traversalOptions, _buildOptions);
        _player = new GridSequencePlayerV2(MidiDevices.Output, (FourBitNumber)0, _clock);
        _moleculePlayback = new MoleculePlaybackViewModel();
        _moleculePlayback.BaseTicPixel = 28.0;

        MidiDevices.Input.EventReceived += OnMidiEventReceived;
        _clock.Pulse += OnClockPulse;
        _player.ActivationChanged += OnActiveStepChanged;
    }

    private void OnActiveStepChanged(object sender, SequenceActivationChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _moleculePlayback.UpdateFromSequence(_model.Graph, e.ActiveAtoms.Select(a => a.Index));
        });
    }


    private int _stepWithinSequence = 0;
    private int _stepIndex;

    private void OnClockPulse(int stepWithinSequence, int pulsesPerStep)
    {
        if (stepWithinSequence < _stepWithinSequence)
        {
            // new step
            if (HasSequence)
            {
                foreach (var led in StepLeds)
                    led.SetCurrent(led.Index == _stepIndex);
                _stepIndex++;
                _stepIndex = _stepIndex % StepLeds.Count;
            }
        }
        _stepWithinSequence = stepWithinSequence;

    }

    private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _clock.Reset();
            _player.Start(true);
            _tickCount = 0;
            _stepIndex = 0;
            _stepWithinSequence = 0;
        }

        if (e.Event is StopEvent)
        {
            _player.Stop();
            _clock.Reset();
            _tickCount = 0;
            _stepIndex = 0;
            _stepWithinSequence = 0;
            LedOn = false;
        }

        if (e.Event is TimingClockEvent)
        {
            _clock.OnClockPulse();
            LedOn = _tickCount % 24 < 12;

            ++_tickCount;
        }
    }


    public void UpdateSequence(MoleculeModel model)
    {
        _model = model;
        UpdateSequence();
    }

    internal void UpdateSequence()
    {
        if (_model == null) return;
        var graph = new MoleculeGraphAdapter(_model.Graph, _packetSettings);
        var builder = new MoleculeSequenceBuilder(graph,
            new BondDurationMapper { Compressed = _settings.Compressed, Quantize = _settings.Quantize },
            _model);
        _sequence = builder.Build(_buildOptions);
        StepLeds = _sequence.Steps.Select(s => s.StepIndex).Distinct().Select((s, i) => new StepLed(i)).ToList();
        _player.SetSequence(_sequence);

        _moleculePlayback.UpdateFromSequence(_model.Graph);

        RaisePropertyChanged(nameof(HasSequence));
    }


}
using Egami.Sequencer.Grid;
using Prism.Mvvm;
using System.Windows.Input;
using Egami.Chemistry.Graph;
using Egami.Chemistry.Sequencer;
using Melanchall.DryWetMidi.Common;
using Prism.Commands;
using static NCDK.AtomTypes.CDKAtomTypeMatcher;

namespace ChemSequencer.ViewModels;

public class PlayerSettings : BindableBase
{
    internal const GridDivision DefaultDivision = GridDivision.Eighth;
    private readonly MoleculePlayer _player;
    private readonly MidiClockGrid _clock;
    private readonly TraversalOptions _traversalOptions;
    private readonly MoleculeSequenceBuildOptions _buildOptions;
    private readonly PacketSettings _packetSettings;

    private int _divisionAsIntAsInt = (int)DefaultDivision;


    public PlayerSettings(MidiClockGrid clock, MoleculePlayer player, TraversalOptions traversalOptions,
        MoleculeSequenceBuildOptions buildOptions, PacketSettings packetSettings)
    {
        _clock = clock;
        _player = player;
        _traversalOptions = traversalOptions;
        _buildOptions = buildOptions;
        _packetSettings = packetSettings;
        ToggleCompressedCommand = new DelegateCommand(() => Compressed = !Compressed);
        ToggleQuantizeCommand = new DelegateCommand(() => Quantize = !Quantize);
        TogglePolyphonicCommand = new DelegateCommand(() => Polyphonic = !Polyphonic);
        ToggleMarkBacktrackingCommand = new DelegateCommand(() => MarkBacktracking = !MarkBacktracking);
        ToggleBacktrackInvertPitchbendCommand =
            new DelegateCommand(() => BacktrackInvertPitchbend = !BacktrackInvertPitchbend);
    }

    public GridDivision Division => (GridDivision)_divisionAsIntAsInt;

    public int DivisionAsInt
    {
        get => _divisionAsIntAsInt;
        set
        {
            if (SetProperty(ref _divisionAsIntAsInt, value))
            {
                _clock.SetDivision((GridDivision)value);
                RaisePropertyChanged(nameof(Division));
            }
        }
    }

    public PitchSelect PitchSelect => (PitchSelect)_pitchSelectInt;

    private int _pitchSelectInt = (int)PitchSelect.Wavelength1;
    public int PitchSelectInt
    {
        get => _pitchSelectInt;
        set
        {
            if (SetProperty(ref _pitchSelectInt, value))
            {
                _packetSettings.PitchSelect = (PitchSelect)value;
                RaisePropertyChanged(nameof(PitchSelect));
                _player.UpdateSequence();
            }
        }
    }

    public PitchRange PitchRange => (PitchRange)_pitchRangeInt;
    private int _pitchRangeInt = (int)PitchRange.Piano;

    public int PitchRangeInt
    {
        get => _pitchRangeInt;
        set
        {
            if (SetProperty(ref _pitchRangeInt, value))
            {
                _packetSettings.PitchRange = (PitchRange)value;
                RaisePropertyChanged(nameof(PitchRange));
                _player.UpdateSequence();
            }
        }
    }

    #region Duration

    private bool _compressed = true;
    public bool Compressed
    {
        get => _compressed;
        set
        {
            if (SetProperty(ref _compressed, value) && _player.Model != null)
            {
                _player.UpdateSequence();
            }
        }
    }

    private bool _quantize = true;
    public bool Quantize
    {
        get => _quantize;
        set
        {
            if (SetProperty(ref _quantize, value) && _player.Model != null)
            {
                _player.UpdateSequence();
            }
        }
    }
    public ICommand ToggleCompressedCommand { get; }
    public ICommand ToggleQuantizeCommand { get; }

    #endregion

    #region Traversal

    public TraversalMode TraversalMode => _buildOptions.Mode;

    public int TraversalModeValue
    {
        get => (int)_buildOptions.Mode;
        set
        {
            var mode = (TraversalMode)value;
            if (mode != _buildOptions.Mode)
            {
                _buildOptions.Mode = (TraversalMode)value;
                RaisePropertyChanged(nameof(TraversalMode));
                _player.UpdateSequence();
            }
        }
    }


    public int MaxRevisitsPerRow
    {
        get => _traversalOptions.MaxRevisitsPerNode;
        set
        {
            if (_traversalOptions.MaxRevisitsPerNode != value)
            {
                _traversalOptions.MaxRevisitsPerNode = value;
                _player.UpdateSequence();
            }
        }
    }

    public int BacktrackMinDepth
    {
        get => _traversalOptions.BacktrackMinDepth;
        set
        {
            if (_traversalOptions.BacktrackMinDepth != value)
            {
                _traversalOptions.BacktrackMinDepth = value;
                _player.UpdateSequence();
            }
        }
    }

    public BacktracePolicy BacktracePolicy => _traversalOptions.BacktrackPolicy;

    public int BacktracePolicyInt
    {
        get => (int)_traversalOptions.BacktrackPolicy;
        set
        {
            var policy = (BacktracePolicy)value;
            if (policy != _traversalOptions.BacktrackPolicy)
            {
                _traversalOptions.BacktrackPolicy = policy;
                RaisePropertyChanged(nameof(BacktracePolicy));
                RaisePropertyChanged(nameof(BacktracePolicyInt));
                _player.UpdateSequence();
            }
        }
    }
    #endregion

    #region Sequencer Options

    public bool Polyphonic
    {
        get => _buildOptions.Polyphonic;
        set
        {
            if (value != _buildOptions.Polyphonic)
            {
                _buildOptions.Polyphonic = value;
                _player.UpdateSequence();
                RaisePropertyChanged(nameof(Polyphonic));
            }
        }
    }

    public ICommand TogglePolyphonicCommand { get; }


    #endregion

    #region Backtracking

    public bool MarkBacktracking
    {
        get => _buildOptions.MarkBacktracking;
        set
        {
            if (value != _buildOptions.MarkBacktracking)
            {
                _buildOptions.MarkBacktracking = value;
                _player.UpdateSequence();
                RaisePropertyChanged(nameof(MarkBacktracking));
            }
        }
    }

    public ICommand ToggleMarkBacktrackingCommand { get; }

    public double BacktrackVelocityScale
    {
        get => _buildOptions.BacktrackVelocityScale;
        set
        {
            if (value != _buildOptions.BacktrackVelocityScale)
            {
                _buildOptions.BacktrackVelocityScale = value;
                _player.UpdateSequence();
                RaisePropertyChanged(nameof(BacktrackVelocityScale));
            }
        }
    }

    public int BacktrackMarkerCcNumber
    {
        get => _buildOptions.BacktrackMarkerCcNumber;
        set
        {
            var newValue = (SevenBitNumber)value;
            if (newValue != _buildOptions.BacktrackMarkerCcNumber)
            {
                _buildOptions.BacktrackMarkerCcNumber = newValue;
                _player.UpdateSequence();
                RaisePropertyChanged(nameof(BacktrackMarkerCcNumber));
            }
        }
    }

    public int BacktrackMarkerCcValue
    {
        get => _buildOptions.BacktrackMarkerCcValue;
        set
        {
            var newValue = (SevenBitNumber)value;
            if (newValue != _buildOptions.BacktrackMarkerCcValue)
            {
                _buildOptions.BacktrackMarkerCcValue = newValue;
                _player.UpdateSequence();
                RaisePropertyChanged(nameof(BacktrackMarkerCcValue));
            }
        }
    }

    public bool BacktrackInvertPitchbend
    {
        get => _buildOptions.BacktrackInvertPitchbend;
        set
        {
            if (value != _buildOptions.BacktrackInvertPitchbend)
            {
                _buildOptions.BacktrackInvertPitchbend = value;
                _player.UpdateSequence();
                RaisePropertyChanged(nameof(BacktrackInvertPitchbend));
            }
        }
    }

    public ICommand ToggleBacktrackInvertPitchbendCommand { get; }

    public int BacktrackPitchbendOffset
    {
        get => _buildOptions.BacktrackPitchbendOffset;
        set
        {
            if (value != _buildOptions.BacktrackPitchbendOffset)
            {
                _buildOptions.BacktrackPitchbendOffset = value;
                _player.UpdateSequence();
                RaisePropertyChanged(nameof(BacktrackPitchbendOffset));
            }
        }
    }

    #endregion

}
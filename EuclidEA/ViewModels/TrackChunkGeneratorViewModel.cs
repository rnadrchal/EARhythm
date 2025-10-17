using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Egami.Rhythm;
using Egami.Rhythm.Core;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Midi.Generation;
using Egami.Rhythm.Pattern;
using EuclidEA.ViewModels.Pitch;
using Microsoft.Win32;
using Prism.Commands;

namespace EuclidEA.ViewModels;

public class TrackChunkGeneratorViewModel : RhythmGeneratorViewModel
{
    protected override IRhythmGenerator Generator => new TrackChunkRhythmGenerator();
    public override string Name => "Track Chunk";

    private string _filePath;
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                RaisePropertyChanged(nameof(FileName));
            }
        }
    }

    public string FileName => System.IO.Path.GetFileName(_filePath);

    public ObservableCollection<TrackRhythmPattern> Patterns { get;  } = new();

    // No Pitch Generator needed for this Rhythm Generator
    public override IPitchGeneratorViewModel PitchGenerator => null;

    public ICommand LoadCommand { get; private set; }

    public TrackChunkGeneratorViewModel()
    {
        LoadCommand = new DelegateCommand(Load);
    }

    protected override RhythmPattern Generate(RhythmContext context)
    {
        var pattern = SelectedPattern?.RhythmPattern;
        if (pattern == null) return null;
        pattern.Hits = pattern.Hits.Take(context.StepsTotal).ToArray();
        pattern.Lengths = pattern.Lengths.Take(context.StepsTotal).ToArray();
        pattern.Pitches = pattern.Pitches.Take(context.StepsTotal).ToArray();
        pattern.Velocities = pattern.Velocities.Take(context.StepsTotal).ToArray();
        pattern.StepsTotal = context.StepsTotal;
        return pattern;
    }

    private int _selectedPatternIndex = 0;
    public int SelectedPatternIndex
    {
        get => _selectedPatternIndex;
        set
        {
            if (SetProperty(ref _selectedPatternIndex, value))
            {
                RaisePropertyChanged(nameof(SelectedPattern));
            }
        }
    }

    private Visibility _patternSelectorVisibility = Visibility.Collapsed;

    public Visibility PatternSelectorVisibility
    {
        get => _patternSelectorVisibility;
        private set => SetProperty(ref _patternSelectorVisibility, value);
    }

    public TrackRhythmPattern SelectedPattern
    {
        get => (_selectedPatternIndex >= 0 && _selectedPatternIndex < Patterns.Count) ? Patterns[_selectedPatternIndex] : null;
    }

    private void Load()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "MIDI-Files (*.mid;*.midi)|*.mid;*.midi|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            FilePath = dlg.FileName;

            var generator = (TrackChunkRhythmGenerator)Generator;

            generator.Load(FilePath, new RhythmContext
            {
                StepsTotal = 64,
                DefaultVelocity = 100,
                Meter = new Meter(4, 4),
                Timebase = new Timebase(4),
                TempoBpm = 120.0
            });

            Patterns.Clear();
            Patterns.AddRange(generator.TrackPatterns);
            SelectedPatternIndex = 0;
            RaisePropertyChanged(nameof(SelectedPattern));
            PatternSelectorVisibility = Patterns.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
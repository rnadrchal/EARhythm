using System;
using System.Collections.Generic;
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

namespace EuclidEA.ViewModels.Rhythm;

public class TrackChunkGeneratorViewModel : RhythmGeneratorViewModel
{
    protected override IRhythmGenerator Generator => new TrackChunkRhythmGenerator();

    private TrackChunkRhythmGenerator TrackChunkGenerator => (TrackChunkRhythmGenerator)Generator;
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

    public ObservableCollection<NamedTrackViewModel> Tracks { get;  } = new();

    // No Pitch Generator needed for this Rhythm Generator
    public override IPitchGeneratorViewModel PitchGenerator => null;

    public ICommand LoadCommand { get; private set; }

    public TrackChunkGeneratorViewModel()
    {
        LoadCommand = new DelegateCommand(Load);
        TrackChunkGenerator.Loaded += OnTracksLoaded;
    }

    private void OnTracksLoaded(object? sender, Dictionary<string, Sequence> tracks)
    {
    }

    protected override Sequence Generate(RhythmContext context)
    {
        var sequence = SelectedTrack?.Sequence;
        if (sequence == null)
        {
            MessageBox.Show("No track selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return new Sequence(0);
        }

        sequence.Steps = sequence.Steps.Take(context.StepsTotal).ToList();
        sequence.StepsTotal = context.StepsTotal;
        return sequence;
    }

    private int _selectedTrackIndex = 0;
    public int SelectedTrackIndex
    {
        get => _selectedTrackIndex;
        set
        {
            if (SetProperty(ref _selectedTrackIndex, value))
            {
                RaisePropertyChanged(nameof(SelectedTrack));
            }
        }
    }

    private Visibility _trackSelectorVisibility = Visibility.Collapsed;

    public Visibility TrackSelectorVisibility
    {
        get => _trackSelectorVisibility;
        private set => SetProperty(ref _trackSelectorVisibility, value);
    }

    public NamedTrackViewModel? SelectedTrack
    {
        get => _selectedTrackIndex >= 0 && _selectedTrackIndex < Tracks.Count ? Tracks[_selectedTrackIndex] : null;
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
                Meter = new Meter(4, 4),
                Timebase = new Timebase(4),
                TempoBpm = 120.0
            });

            Tracks.Clear();
            Tracks.AddRange(generator.Tracks.Select(t => new NamedTrackViewModel(t.Value, t.Key)));
            if (Tracks.Count > 0)
            {
                SelectedTrackIndex = 0;
                RaisePropertyChanged(nameof(SelectedTrack));
            }
            TrackSelectorVisibility = Tracks.Count > 1 ? Visibility.Visible : Visibility.Collapsed;


        }
    }
}
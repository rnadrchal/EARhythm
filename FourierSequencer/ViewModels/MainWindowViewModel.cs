using FourierSequencer.Models;
using Microsoft.Win32;
using Prism.Mvvm;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FourierSequencer.Services;
using Prism.Commands;
using Prism.Events;
using Syncfusion.Windows.Shared;

namespace FourierSequencer.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;

    private string _title = "Fourier";

    public string Title
    {
        get { return _title; }
        set { SetProperty(ref _title, value); }
    }

    private readonly FourierSequencerModel _pitchSequencer;
    private readonly FourierSequencerModel _velocitySequencer;
    private readonly FourierSequencerModel _pitchbendSequencer;
    private readonly FourierSequencerModel _controlChangeSequencer;

    public FourierSequencerModel PitchSequencer => _pitchSequencer;
    public FourierSequencerModel VelocitySequencer => _velocitySequencer;
    public FourierSequencerModel PitchbendSequencer => _pitchbendSequencer;
    public FourierSequencerModel ControlChangeSequencer => _controlChangeSequencer;

    public ICommand SavePresetCommand { get; }
    public ICommand LoadPresetCommand { get; }

    public MainWindowViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        _pitchSequencer = new FourierSequencerModel(SequencerTarget.Pitch, _eventAggregator);
        _pitchSequencer.IsActive = true;
        _velocitySequencer = new FourierSequencerModel(SequencerTarget.Velocity, eventAggregator);
        _velocitySequencer.Harmonics = 0;
        _velocitySequencer.IsActive = false;
        _pitchbendSequencer = new FourierSequencerModel(SequencerTarget.Pitchbend, eventAggregator);
        _pitchbendSequencer.IsActive = false;
        _controlChangeSequencer = new FourierSequencerModel(SequencerTarget.ControlChange, eventAggregator);
        _controlChangeSequencer.IsActive = false;

        SavePresetCommand = new Prism.Commands.DelegateCommand(SavePreset);
        LoadPresetCommand = new Prism.Commands.DelegateCommand(LoadPreset);
    }

    private async void SavePreset()
    {
        var userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Egami",
            "Fourier");
        if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
        var dlg = new SaveFileDialog()
        {
            InitialDirectory = userDir,
            Filter = "Preset (*.fourier)|*.fourier",
            DefaultExt = ".fourier", // Default-Endung
            AddExtension = true, // bei fehlender Endung automatisch anhängen
            Title = "Save Preset"
        };
        if (dlg.ShowDialog() == true)
        {
            await FourierSequencerStorage.SaveAsync(dlg.FileName, _pitchSequencer);
        }
    }

    private async void LoadPreset()
    {
        var userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Egami",
            "Fourier");
        if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
        var dlg = new OpenFileDialog()
        {
            InitialDirectory = userDir,
            Filter = "Preset (*.fourier)|*.fourier",
            Title = "Load Preset"
        };

        if (dlg.ShowDialog() == true)
        {
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                await FourierSequencerStorage.LoadAsync(dlg.FileName, _pitchSequencer);
            });
        }

    }

    public void Cleanup()
    {
        _pitchbendSequencer.Cleanup();
        _velocitySequencer.Cleanup();
        _pitchbendSequencer.Cleanup();
        _controlChangeSequencer.Cleanup();
    }
}
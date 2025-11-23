using FourierSequencer.Models;
using Microsoft.Win32;
using Prism.Mvvm;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FourierSequencer.Services;

namespace FourierSequencer.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private string _title = "Fourier";

    public string Title
    {
        get { return _title; }
        set { SetProperty(ref _title, value); }
    }

    private readonly FourierSequencerModel _sequencer;
    public FourierSequencerModel Sequencer => _sequencer;

    public ICommand SavePresetCommand { get; }
    public ICommand LoadPresetCommand { get; }

    public MainWindowViewModel(FourierSequencerModel sequencer)
    {
        _sequencer = sequencer;
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
            await FourierSequencerStorage.SaveAsync(dlg.FileName, _sequencer);
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
                await FourierSequencerStorage.LoadAsync(dlg.FileName, _sequencer);
            });
        }

    }
}
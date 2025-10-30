using Egami.Rhythm.Midi;
using ImageSequencer.Events;
using ImageSequencer.Models;
using ImageSequencer.Services;
using ImageSequencer.Views;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Win32;
using Prism.Events;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace ImageSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ApplicationSettings _applicationSettings;
        private readonly OutsourceWindow _outsource;
        public ApplicationSettings ApplicationSettings => _applicationSettings;

        private readonly VisitViewer _visitViewer;
        public VisitViewer VisitViewer => _visitViewer;
        private readonly ImageViewer _imageViewer;
        public ImageViewer ImageViewer => _imageViewer;

        private string _title = "Jackson's Dance";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private bool _ledTick = false;
        public bool LedTick
        {
            get { return _ledTick; }
            set { SetProperty(ref _ledTick, value); }
        }

        private string _stepInfo = string.Empty;
        public string StepInfo
        {
            get => _stepInfo;
            set => SetProperty(ref _stepInfo, value);
        }

        private bool _isOutsourceVisible;

        public bool IsOutsourceVisible
        {
            get => _isOutsourceVisible;
            set => SetProperty(ref _isOutsourceVisible, value);
        }

        private bool _isOutsourceMaximized;

        public bool IsOutsourceMaximized
        {
            get => _isOutsourceMaximized;
            set => SetProperty(ref _isOutsourceMaximized, value);
        }

        public ICommand ToggleVisitCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand FastForwardCommand { get; }
        public ICommand ShowOutsourceCommand { get; }
        public ICommand HideOutsourceCommand { get; }

        public ICommand MaximizeOutsourceCommand { get; }
        public ICommand MinimizeOutsourceCommand { get; }

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand RevertToOriginalCommand { get; }

        public MainWindowViewModel(ApplicationSettings applicationSettings, VisitViewer visitViewer, ImageViewer imageViewer, IEventAggregator eventAggregator)
        {
            _applicationSettings = applicationSettings;
            _visitViewer = visitViewer;
            _imageViewer = imageViewer;
            _eventAggregator = eventAggregator;

            _outsource = new OutsourceWindow
            {
                DataContext = this
            };

            ToggleVisitCommand =
                new DelegateCommand(_ => ApplicationSettings.IsVisiting = !ApplicationSettings.IsVisiting);

            MidiDevices.Input.EventReceived += OnClockEvent;

            _eventAggregator.GetEvent<StepEvent>().Subscribe(step =>
            {
                StepInfo = step.ToString();
            });

            ResetCommand = new DelegateCommand(_visitViewer.ResetCommand.Execute);
            FastForwardCommand = new DelegateCommand(_visitViewer.FastForwardCommand.Execute);
            ShowOutsourceCommand = new DelegateCommand(_ => ShowOutsource());
            HideOutsourceCommand = new DelegateCommand(_ => HideOutsource());
            MaximizeOutsourceCommand = new DelegateCommand(_ => MaximizeOutsource());
            MinimizeOutsourceCommand = new DelegateCommand(_ => MinimizeOutsource());
            SavePresetCommand = new DelegateCommand(_ => SavePreset());
            LoadPresetCommand = new DelegateCommand(_ => LoadPreset());
        }

        private void ShowOutsource()
        {
            _outsource.Show();
            IsOutsourceVisible = true;
        }

        private void HideOutsource()
        {
            if (_outsource.WindowState == WindowState.Maximized)
            {
                MinimizeOutsource();
            }
            _outsource.Hide();
            IsOutsourceVisible = false;
        }

        private void MaximizeOutsource()
        {
            _outsource.WindowState = WindowState.Maximized;
            _outsource.WindowStyle = WindowStyle.None;
            IsOutsourceMaximized = true;
        }

        private void MinimizeOutsource()
        {
            _outsource.WindowState = WindowState.Normal;
            _outsource.WindowStyle = WindowStyle.ToolWindow;
            IsOutsourceMaximized = false;
        }

        private ulong _tickCount;
        private void OnClockEvent(object sender, MidiEventReceivedEventArgs e)
        {
            if (e.Event is StartEvent)
            {
                _tickCount = 0;
            }

            if (e.Event is StopEvent)
            {
                LedTick = false;
            }

            if (e.Event is TimingClockEvent)
            {
                if (_tickCount % 24 == 0)
                {
                    LedTick = true;
                }
                else if (_tickCount % 24 == 12)
                {
                    LedTick = false;
                }
                ++_tickCount;
            }
        }

        public void CloseOutsource()
        {
            if (_outsource != null) _outsource.Close();
        }

        private void SavePreset()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var userDir = Path.Combine(docs, "Egami", "JacksonsDance", "User");
            if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
            var dlg = new SaveFileDialog()
            {
                InitialDirectory = Path.Combine(docs, "Egami", "JacksonsDance", "User"),
                Filter = "Preset (*.json)|*.json",
                DefaultExt = ".json",    // Default-Endung
                AddExtension = true,     // bei fehlender Endung automatisch anhängen
                Title = "Save Preset"
            };
            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
                // zusätzliche Sicherheit: bei fehlender/anderen Endung .json erzwingen
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    path = Path.ChangeExtension(path, ".json");
                }

                SettingsPersistence.Save( _applicationSettings, Path.Combine(dlg.RootDirectory, path));
            }
        }

        public void LoadPreset()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var userDir = Path.Combine(docs, "Egami", "JacksonsDance", "User");
            if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
            var dlg = new OpenFileDialog()
            {
                InitialDirectory = userDir,
                Filter = "Preset (*.json)|*.json",
                Title = "Load Preset"
            };
            if (dlg.ShowDialog() == true)
            {
                SettingsPersistence.Load(_applicationSettings, dlg.FileName, loadBitmap: false);

                // wenn FilePath erreichbar, Bild über ImageViewer laden
                if (!string.IsNullOrWhiteSpace(_applicationSettings.FilePath) && File.Exists(_applicationSettings.FilePath))
                {

                    _imageViewer.LoadBitmap(_applicationSettings.FilePath);
                }
            }
        }
    }
}

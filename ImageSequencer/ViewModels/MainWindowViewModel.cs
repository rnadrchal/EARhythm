using System;
using System.Windows;
using System.Windows.Input;
using Egami.Rhythm.Midi;
using ImageSequencer.Events;
using ImageSequencer.Models;
using ImageSequencer.Views;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

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

        private string _title = "Image Sequencer";
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
        }

        private void ShowOutsource()
        {
            _outsource.Show();
            IsOutsourceVisible = true;
        }

        private void HideOutsource()
        {
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
            _outsource.WindowStyle = WindowStyle.SingleBorderWindow;
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
    }
}

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Egami.Imaging.Midi;
using Egami.Imaging.Visiting;
using Egami.Rhythm.Common;
using Egami.Rhythm.Midi;
using ImageSequencer.Events;
using ImageSequencer.Models;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;
using Prism.Mvvm;
using Syncfusion.Windows.Shared;

namespace ImageSequencer.ViewModels;

public class VisitViewer : BindableBase, IDisposable
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ApplicationSettings _applicationSettings;
    private bool _isPaused = false;
    public ApplicationSettings ApplicationSettings => _applicationSettings;

    private IBitmapVisitor _visitor = null;
    public ICommand ResetCommand { get; }
    public ICommand FastForwardCommand { get; }

    public VisitViewer(ApplicationSettings applicationSettings, IEventAggregator eventAggregator)
    {
        _applicationSettings = applicationSettings;
        _eventAggregator = eventAggregator;
        _applicationSettings.PropertyChanged += OnApplicationSettingsChanged;
        if (_applicationSettings.Bitmap != null)
        {
            _visitor = BitmapVisitorFactory.Create(_applicationSettings.VisitorType, _applicationSettings.Bitmap);
        }

        MidiDevices.Input.EventReceived += OnMidiEventReceived;
        ResetCommand = new DelegateCommand(_ => Reset());
        FastForwardCommand = new DelegateCommand(_ => FastForward());
        _eventAggregator.GetEvent<ResetRequest>().Subscribe(Reset);
    }

    private ulong _tick = 0;
    private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _tick = 0;
        }
        if (e.Event is TimingClockEvent)
        {
            if (_applicationSettings.IsVisiting && _tick % (ulong)(96 / _applicationSettings.Divider) == 0)
            {
                if (_visitor != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _visitor.Next();
                    });
                    
                }
            }
            ++_tick;
        }
    }

    private void OnApplicationSettingsChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ApplicationSettings.Bitmap) ||
            e.PropertyName == nameof(ApplicationSettings.VisitorType))
        {
            Reset();
            //SetVisitor();
        }

        if (e.PropertyName == nameof(ApplicationSettings.IsVisiting))
        {
            if (_applicationSettings.IsVisiting && _visitor == null)
            {
                SetVisitor();
            }
        }
    }

    private void SetVisitor()
    {
        var isVisiting = _applicationSettings.IsVisiting;
        if (_visitor != null)
        {
            _visitor.Visited -= OnVisited;
        }
        if (_applicationSettings.Bitmap != null)
        {
            var x = RandomProvider.Get(null).Next(0, (int)_applicationSettings.Bitmap.Width);
            var y = RandomProvider.Get(null).Next(0, (int)_applicationSettings.Bitmap.Height);
            _visitor = BitmapVisitorFactory.Create(_applicationSettings.VisitorType, _applicationSettings.Bitmap, x, y);
            _visitor.Visited += OnVisited;
        }
        else
        {
            _visitor = null;
        }
    }

    private byte? _lastNote;

    private void OnVisited(object sender, VisitorEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_applicationSettings.RenderTarget != null)
            {
                if (!_isPaused)
                    HandleMidiEvent(e);
                _applicationSettings.RenderTarget.SetPixel(e.X, e.Y, e.Color);
            }
        });
    }

    private void HandleMidiEvent(VisitorEventArgs e)
    {

        var step = new StepInfo();

        if (_applicationSettings.SendPitchbendOn)
        {
            var pitchbend = (int)Math.Round(ColorToCvFactory.Create(_applicationSettings.PitchbendColorToCvType,
                _applicationSettings.PitchbendBaseColor).Convert(e.Color) / 127.0 * 16383);
            MidiDevices.Output.SendEvent(new PitchBendEvent((ushort)pitchbend));
            step.Pitchbend = pitchbend;;
        }

        if (_applicationSettings.SendControlChangeOn)
        {
            var ccValue = ColorToCvFactory.Create(_applicationSettings.ControlChangeColorToCvType,
                _applicationSettings.ControlChangeBaseColor).Convert(e.Color);
            MidiDevices.Output.SendEvent(new ControlChangeEvent((SevenBitNumber)_applicationSettings.ControlChangeNumber,
                (SevenBitNumber)ccValue));
            step.ControlChangeNumber = _applicationSettings.ControlChangeNumber;
            step.ControlChangeValue = ccValue;
        }

        if (_applicationSettings.SendNoteOn)
        {
            var pitch = ColorToCvFactory.Create(_applicationSettings.PitchColorToCvType,
                _applicationSettings.VelocityBaseColor).Convert(e.Color);
            pitch = (byte)(_applicationSettings.TonalRangeLower +
                           (pitch / 127.0) * (_applicationSettings.TonalRangeUpper - _applicationSettings.TonalRangeLower));
            var velocity = ColorToCvFactory.Create(_applicationSettings.VelocityColorToCvType,
                _applicationSettings.VelocityBaseColor).Convert(e.Color);

            if (_lastNote.HasValue)
            {
                if (_applicationSettings.Legato)
                {
                    if (_lastNote.Value != pitch)
                    {
                        MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0));
                        MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity));
                        step.NoteNumber = pitch;
                        step.Velocity = velocity;
                        _lastNote = pitch;
                    }
                    // Bei Pitch-Gleichheit: nichts tun, Note bleibt aktiv
                }
                else
                {
                    MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0));
                    MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity));
                    step.NoteNumber = pitch;
                    step.Velocity = velocity;
                    _lastNote = pitch;
                }
            }
            else
            {
                MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity));
                step.NoteNumber = pitch;
                step.Velocity = velocity;
                _lastNote = pitch;
            }
        }
        _eventAggregator.GetEvent<StepEvent>().Publish(step);
    }

    private void Reset()
    {
        _applicationSettings.ClearRenderTarget();
        if (_lastNote != null)
        {
            MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0));
        }
        SetVisitor();
    }

    private void FastForward()
    {
        _isPaused = true;
        var stepSize = _applicationSettings.Bitmap.Width * _applicationSettings.Bitmap.Height / 100;
        for (var i = 0; i < stepSize; ++i)
        {
            _visitor.Next();
        }

        _isPaused = false;
    }

    public void Dispose()
    {
        if (_lastNote != null)
        {
            MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0));
        }

        MidiDevices.Input.EventReceived -= OnMidiEventReceived;
    }
}
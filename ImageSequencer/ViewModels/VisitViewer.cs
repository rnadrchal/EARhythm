using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Egami.Imaging.Extensions;
using Egami.Imaging.Midi;
using Egami.Imaging.Visiting;
using Egami.Rhythm.Common;
using Egami.Rhythm.Midi;
using ImageSequencer.Models;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Mvvm;

namespace ImageSequencer.ViewModels;

public class VisitViewer : BindableBase
{
    private readonly ApplicationSettings _applicationSettings;
    public ApplicationSettings ApplicationSettings => _applicationSettings;

    private IBitmapVisitor _visitor = null;

    public VisitViewer(ApplicationSettings applicationSettings)
    {
        _applicationSettings = applicationSettings;
        _applicationSettings.PropertyChanged += OnApplicationSettingsChanged;
        if (_applicationSettings.Bitmap != null)
        {
            _visitor = BitmapVisitorFactory.Create(_applicationSettings.VisitorType, _applicationSettings.Bitmap);
        }

        MidiDevices.Input.EventReceived += OnMidiEventReceived;
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
            SetVisitor();
        }

        if (e.PropertyName == nameof(ApplicationSettings.IsVisiting))
        {
            if (_applicationSettings.IsVisiting)
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
                HandleMidiEvent(e);
                _applicationSettings.RenderTarget.SetPixel(e.X, e.Y, e.Color);
            }
        });
    }

    private void HandleMidiEvent(VisitorEventArgs e)
    {
        if (_applicationSettings.SendNoteOn)
        {
            var pitch = new BaseColorToCv(BaseColor.Red).Convert(e.Color);
            var velocity = new LuminanceToCv().Convert(e.Color);

            if (_lastNote.HasValue)
            {
                if (_applicationSettings.Legato)
                {
                    if (_lastNote.Value != pitch)
                    {
                        MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0));
                        MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity));
                        _lastNote = pitch;
                    }
                    // Bei Pitch-Gleichheit: nichts tun, Note bleibt aktiv
                }
                else
                {
                    MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0));
                    MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity));
                    _lastNote = pitch;
                }
            }
            else
            {
                MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity));
                _lastNote = pitch;
            }
        }
    }
}
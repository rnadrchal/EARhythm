using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;
using Prism.Mvvm;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows.Media.Imaging;
using Blattwerk.Common;
using Prism.Commands;
using System.Linq;
using System.Collections.ObjectModel;
using Blattwerk.Events;
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;

namespace Blattwerk.ViewModels;

public enum ImageDirection
{
    [Display(Name="LTR")]
    Ltr,
    [Display(Name = "RTL")]
    Rtl,
    [Display(Name = "Top down")]
    TopDown,
    [Display(Name = "Bottom up")]
    BottomUp
}

public class ImageConfiguration : BindableBase, IDisposable
{
    private readonly IEventAggregator _eventAggregator;

    private long _tickCount = 0;

    private Image<Rgba32>? _image;

    private BitmapSource? _bitmap;

    private ImageDirection _direction = ImageDirection.Ltr;

    private int _columnOrRow =0;
    public int ColumnOrRow
    {
        get => _columnOrRow;
        set
        {
            if (SetProperty(ref _columnOrRow, value))
            {
                UpdateSegmentStats();
            }
        }
    }

    private int _segmentCount =32;
    public int SegmentCount
    {
        get => _segmentCount;
        set
        {
            if (SetProperty(ref _segmentCount, value))
            {
                UpdateSegmentStats();
            }
        }
    }

    public int[] SegmentCounts => new[] {8,16,32,64,128 };

    private int _segmentCountIndex =2;

    public int SegmentCountIndex
    {
        get => _segmentCountIndex;
        set
        {
            if (SetProperty(ref _segmentCountIndex, value))
            {
                SegmentCount = SegmentCounts[value];
            }
        }
    }

    private int _maxPosition =0;

    public int MaxPosition
    {
        get => _maxPosition;
        set => SetProperty(ref _maxPosition, value);
    }

    public DelegateCommand<ImageDirection?> SetDirectionCommand { get; }
    public ImageDirection Direction
    {
        get => _direction;
        set
        {
            if (SetProperty(ref _direction, value))
            {
                switch (value)
                {
                    case ImageDirection.Rtl:
                    case ImageDirection.Ltr:
                        MaxPosition = (int)(_bitmap?.Height -1 ??0);
                        break;
                    case ImageDirection.TopDown:
                    case ImageDirection.BottomUp:
                        MaxPosition = (int)(_bitmap?.Width -1 ??0);
                        break;
                }

                if (ColumnOrRow >= _maxPosition) ColumnOrRow = _maxPosition;
                else UpdateSegmentStats();
            }
        }
    }

    public BitmapSource? Bitmap
    {
        get => _bitmap;
        private set => SetProperty(ref _bitmap, value);
    }

    // Backing array of computed stats (kept for internal use)
    private SegmentColorStats[] _stats = Array.Empty<SegmentColorStats>();

    // Expose the segment stats as an observable collection for binding (one item per segment)
    private readonly ObservableCollection<SegmentColorStats> _segments = new();
    public ReadOnlyObservableCollection<SegmentColorStats> Segments { get; }

    // Cached float arrays to avoid re-creating arrays on every getter call
    private float[] _a = Array.Empty<float>();
    private float[] _r = Array.Empty<float>();
    private float[] _g = Array.Empty<float>();
    private float[] _b = Array.Empty<float>();
    private float[] _y = Array.Empty<float>();
    private float[] _m = Array.Empty<float>();
    private float[] _c = Array.Empty<float>();
    private float[] _saturation = Array.Empty<float>();
    private float[] _luminance = Array.Empty<float>();
    private float[] _recipWeightedBrightness = Array.Empty<float>();

    public float[] A => _a;
    public float[] R => _r;
    public float[] G => _g;
    public float[] B => _b;
    public float[] Y => _y;
    public float[] M => _m;
    public float[] C => _c;
    public float[] Saturation => _saturation;
    public float[] Luminance => _luminance;
    public float[] RecipWeightedBrightness => _recipWeightedBrightness;

    private readonly TrackConfiguration _aConfiguration;
    public TrackConfiguration AConfiguration => _aConfiguration;
    private readonly TrackConfiguration _rConfiguration;
    public TrackConfiguration RConfiguration => _rConfiguration;
    private readonly TrackConfiguration _gConfiguration;
    public TrackConfiguration GConfiguration => _gConfiguration;
    private readonly TrackConfiguration _bConfiguration;
    public TrackConfiguration BConfiguration => _bConfiguration;
    private readonly TrackConfiguration _yConfiguration;
    public TrackConfiguration YConfiguration => _yConfiguration;
    private readonly TrackConfiguration _mConfiguration;
    public TrackConfiguration MConfiguration => _mConfiguration;
    private readonly TrackConfiguration _cConfiguration;
    public TrackConfiguration CConfiguration => _cConfiguration;
    private readonly TrackConfiguration _saturationConfiguration;
    public TrackConfiguration SaturationConfiguration => _saturationConfiguration;
    private readonly TrackConfiguration _luminanceConfiguration;
    public TrackConfiguration LuminanceConfiguration => _luminanceConfiguration;
    private readonly TrackConfiguration _brightnessConfiguration;

    public TrackConfiguration BrightnessConfiguration => _brightnessConfiguration;

    public TrackConfiguration[] _configurations;

    public DelegateCommand<TrackConfigurationType> SetTrackConfigurationCommand { get; }

    private readonly ClockDivider[] _dividers = Enum.GetValues<ClockDivider>();

    public ClockDivider Divider => _dividers[_dividerIndex];

    private int _dividerIndex = 2;
    public int DividerIndex
    {
        get => _dividerIndex;
        set
        {
            if (SetProperty(ref _dividerIndex, value))
            {
                RaisePropertyChanged(nameof(Divider));
            }
        }
    }

    private int _channel = 0;

    public int Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    public ImageConfiguration(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        Segments = new ReadOnlyObservableCollection<SegmentColorStats>(_segments);
        SetDirectionCommand = new DelegateCommand<ImageDirection?>(d => Direction = d.HasValue ? d.Value : ImageDirection.Ltr);

        _aConfiguration = new TrackConfiguration(_eventAggregator, "Alpha")
        {
            IsVelocityTrack = true
        };
        _rConfiguration = new TrackConfiguration(_eventAggregator, "Red");
        _gConfiguration = new TrackConfiguration(_eventAggregator, "Green");
        _bConfiguration = new TrackConfiguration(_eventAggregator, "Blue");
        _yConfiguration = new TrackConfiguration(_eventAggregator, "Yellow");
        _mConfiguration = new TrackConfiguration(_eventAggregator, "Magenta");
        _cConfiguration = new TrackConfiguration(_eventAggregator, "Cyan");
        _saturationConfiguration = new TrackConfiguration(_eventAggregator, "Saturation");
        _luminanceConfiguration = new TrackConfiguration(_eventAggregator, "Luminance");
        _brightnessConfiguration = new TrackConfiguration(_eventAggregator, "Brightness")
        {
            IsPitchTrack = true
        };

        _configurations =
        [
            _aConfiguration,
            _rConfiguration,
            _gConfiguration,
            _yConfiguration,
            _bConfiguration,
            _mConfiguration,
            _cConfiguration,
            _saturationConfiguration,
            _luminanceConfiguration,
            _brightnessConfiguration
        ];

        _eventAggregator.GetEvent<TrackConfigurationEvent>().Subscribe(OnTrackConfiguration);

        MidiDevices.Input.EventReceived += OnMidiEventReceived;
    }

    private long _stepCount = 0;

    private int? _lastNote = null;

    private void NoteOff()
    {
        if (_lastNote.HasValue)
        {
            MidiDevices.Output.SendEvent(new NoteOffEvent((SevenBitNumber)_lastNote.Value, (SevenBitNumber)0) { Channel = (FourBitNumber)_channel});
            _lastNote = null;
        }
    }

    private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is StartEvent)
        {
            _tickCount = 0;
            _stepCount = 0;
            NoteOff();
        }

        if (e.Event is StopEvent)
        {
            NoteOff();
        }

        if (e.Event is TimingClockEvent)
        {
            if ((_tickCount % (long)Divider) == 0)
            {
                NoteOff();
                var step = _stepCount % _segmentCount;
                var pitchbendConfig = _configurations.FirstOrDefault(c => c.IsPitchbendTrack);
                if (pitchbendConfig != null)
                {
                    var norm = GetNormalizedValue(pitchbendConfig.Name, (int)step);
                    var pitchbend = pitchbendConfig.Pitchbend.GetValue(norm);
                    MidiDevices.Output.SendEvent(new PitchBendEvent((ushort)pitchbend) { Channel = (FourBitNumber)_channel });
                }

                foreach (var ccConfig in _configurations.Where(c => c.IsControlChangeTrack))
                {
                    var norm = GetNormalizedValue(ccConfig.Name, (int)step);
                    var value = ccConfig.ControlChange.GetValue(norm);
                    MidiDevices.Output.SendEvent(new ControlChangeEvent((SevenBitNumber)ccConfig.ControlChange.Number, (SevenBitNumber)value) { Channel = (FourBitNumber)_channel});
                }

                var pitchConfig = _configurations.Single(c => c.IsPitchTrack);
                var pitchNorm = GetNormalizedValue(pitchConfig.Name, (int)step);
                var pitch = pitchConfig.Pitch.GetValue(pitchNorm);
                var velConfig = _configurations.Single(c => c.IsVelocityTrack);
                var velNorm = GetNormalizedValue(velConfig.Name, (int)step);
                var velocity = velConfig.Velocity.GetValue(velNorm);
                _lastNote = pitch;
                MidiDevices.Output.SendEvent(new NoteOnEvent((SevenBitNumber)pitch, (SevenBitNumber)velocity) { Channel = (FourBitNumber)_channel});

                ++_stepCount;
            }
            ++_tickCount;
        }
    }

    private float GetNormalizedValue(string name, int index)
    {
        if (_bitmap == null) return 0f;
        return name switch
        {
            "Alpha" => _a[index],
            "Red" => _r[index],
            "Green" => _g[index],
            "Blue" => _b[index],
            "Yellow" => _y[index],
            "Magenta" => _m[index],
            "Cyan" => _c[index],
            "Saturation" => _saturation[index],
            "Luminance" => _luminance[index],
            "Brightness" => _recipWeightedBrightness[index],
            _ => throw new ArgumentOutOfRangeException(name)
        };
    }

    private void OnTrackConfiguration((TrackConfigurationType type, string key) e)
    {
        switch (e.type)
        {
            case TrackConfigurationType.Pitch:
                _configurations.Where(c => c.IsPitchTrack && c.Name != e.key).ToList().ForEach(c => c.IsPitchTrack = false);
                break;
            case TrackConfigurationType.Velocity:
                _configurations.Where(c => c.IsVelocityTrack && c.Name != e.key).ToList().ForEach(c => c.IsVelocityTrack = false);
                break;
            case TrackConfigurationType.Pitchbend:
                _configurations.Where(c => c.IsPitchbendTrack && c.Name != e.key).ToList().ForEach(c => c.IsPitchbendTrack = false);
                break;
        }
    }

    // Asynchrone Variante (empfohlen für UI Responsiveness)
    public async System.Threading.Tasks.Task LoadImageAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        _image?.Dispose();
        Bitmap = null;

        try
        {
            using var fs = File.OpenRead(path);
            var img = await Image.LoadAsync<Rgba32>(fs).ConfigureAwait(false);

            // Übergabe auf UI‑Thread nötig für SetProperty / BitmapSource
            Application.Current.Dispatcher.Invoke(() =>
            {
                _image = img;
                Bitmap = ToBitmapSource(img);
                Bitmap?.Freeze();
                ColumnOrRow =0;
                MaxPosition = _image.Width -1;
                Direction = ImageDirection.Ltr;
                UpdateSegmentStats();
            });
        }
        catch (Exception ex)
        {
            _image = null;
            Bitmap = null;
            MessageBox.Show(Application.Current.MainWindow, ex.Message, "Error loading Image", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private static BitmapSource ToBitmapSource(Image<Rgba32> img)
    {
        // Verwende die Frame‑API und ProcessPixelRows / GetRowSpan, statt direkter GetPixelRowSpan-Aufrufe.
        var frame = img.Frames.RootFrame; // oder img.Frames[0]
        int w = frame.Width;
        int h = frame.Height;
        int stride = w *4;
        var pixels = new byte[h * stride];

        // ProcessPixelRows liefert einen Row-Accessor; GetRowSpan(y) liefert die Pixel der Zeile.
        frame.ProcessPixelRows(accessor =>
        {
            for (int y =0; y < h; y++)
            {
                var row = accessor.GetRowSpan(y); // Span<Rgba32>
                int rowOffset = y * stride;
                for (int x =0; x < w; x++)
                {
                    var p = row[x];
                    pixels[rowOffset + x *4 +0] = p.B;
                    pixels[rowOffset + x *4 +1] = p.G;
                    pixels[rowOffset + x *4 +2] = p.R;
                    pixels[rowOffset + x *4 +3] = p.A;
                }
            }
        });

        var wb = new WriteableBitmap(w, h,96,96, System.Windows.Media.PixelFormats.Bgra32, null);
        wb.WritePixels(new System.Windows.Int32Rect(0,0, w, h), pixels, stride,0);
        wb.Freeze();
        return wb;
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;
        Bitmap = null;
    }

    private SegmentColorStats[] GetSegmentStats()
    {
        if (_image == null)
            return Array.Empty<SegmentColorStats>();

        // Extrahiere die gewünschte Linie (Spalte oder Zeile)
        Rgba32[] linePixels = Direction switch
        {
            ImageDirection.Ltr or ImageDirection.Rtl => GetRowPixels(_image, ColumnOrRow),
            ImageDirection.TopDown or ImageDirection.BottomUp => GetColumnPixels(_image, ColumnOrRow),
            _ => throw new InvalidOperationException("Unbekannte Bildrichtung.")
        };

        // Direkt mit Rgba32 arbeiten (ComputeSegmentAverages erwartet jetzt Rgba32)
        var stats = linePixels.ComputeSegmentAverages(SegmentCount);

        // Invertiere die Reihenfolge bei RTL / BottomUp und aktualisiere Index-Felder
        if (Direction == ImageDirection.Rtl || Direction == ImageDirection.BottomUp)
        {
            Array.Reverse(stats);

            for (int i =0; i < stats.Length; i++)
            {
                var s = stats[i];
                stats[i] = new SegmentColorStats
                {
                    Index = i,
                    PixelCount = s.PixelCount,
                    A = s.A,
                    R = s.R,
                    G = s.G,
                    B = s.B,
                    Saturation = s.Saturation,
                    Luminance = s.Luminance,
                    RecipWeightedBrightness = s.RecipWeightedBrightness
                };
            }
        }

        return stats;
    }

    private void UpdateSegmentStats()
    {
        _stats = GetSegmentStats();

        // Update observable collection in-place so bindings see incremental changes
        _segments.Clear();
        foreach (var s in _stats)
            _segments.Add(s);

        // Cache float arrays to avoid allocation every time a getter is accessed
        int n = _stats.Length;
        _a = new float[n];
        _r = new float[n];
        _g = new float[n];
        _b = new float[n];
        _y = new float[n];
        _m = new float[n];
        _c = new float[n];
        _saturation = new float[n];
        _luminance = new float[n];
        _recipWeightedBrightness = new float[n];

        for (int i =0; i < n; i++)
        {
            var s = _stats[i];
            _a[i] = s.A;
            _r[i] = s.R;
            _g[i] = s.G;
            _b[i] = s.B;
            _y[i] = s.Y;
            _m[i] = s.M;
            _c[i] = s.C;
            _saturation[i] = s.Saturation;
            _luminance[i] = s.Luminance;
            _recipWeightedBrightness[i] = s.RecipWeightedBrightness;
        }

        // Notify array properties changed
        RaisePropertyChanged(nameof(A));
        RaisePropertyChanged(nameof(R));
        RaisePropertyChanged(nameof(G));
        RaisePropertyChanged(nameof(B));
        RaisePropertyChanged(nameof(Y));
        RaisePropertyChanged(nameof(M));
        RaisePropertyChanged(nameof(C));
        RaisePropertyChanged(nameof(Saturation));
        RaisePropertyChanged(nameof(Luminance));
        RaisePropertyChanged(nameof(RecipWeightedBrightness));
    }

    /// <summary>
    /// Extrahiert eine Zeile aus einem Image<Rgba32> als Array.
    /// </summary>
    private static Rgba32[] GetRowPixels(Image<Rgba32> image, int row)
    {
        if (row <0 || row >= image.Height)
            throw new ArgumentOutOfRangeException(nameof(row));

        var pixels = new Rgba32[image.Width];
        image.ProcessPixelRows(accessor =>
        {
            var span = accessor.GetRowSpan(row);
            span.CopyTo(pixels);
        });
        return pixels;
    }

    /// <summary>
    /// Extrahiert eine Spalte aus einem Image<Rgba32> als Array.
    /// </summary>
    private static Rgba32[] GetColumnPixels(Image<Rgba32> image, int column)
    {
        if (column <0 || column >= image.Width)
            throw new ArgumentOutOfRangeException(nameof(column));

        var pixels = new Rgba32[image.Height];
        image.ProcessPixelRows(accessor =>
        {
            for (int y =0; y < image.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                pixels[y] = row[column];
            }
        });
        return pixels;
    }
}
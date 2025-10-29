using System;
using System.Windows;
using System.Windows.Input;
using Egami.Imaging.Visiting;
using Prism.Mvvm;
using System.Windows.Media.Imaging;
using Egami.Imaging.Extensions;
using Egami.Imaging.Midi;
using ImageSequencer.Events;
using ImageSequencer.Extensions;
using Prism.Events;

namespace ImageSequencer.Models;

public class ApplicationSettings : BindableBase
{
    private readonly IEventAggregator _aggregator;


    private WriteableBitmap? _original;
    public WriteableBitmap? Original
    {
        get => _original;
        set => SetProperty(ref _original, value);
    }

    private WriteableBitmap? _bitmap;
    public WriteableBitmap? Bitmap
    {
        get => _bitmap;
        set
        {
            if (SetProperty(ref _bitmap, value))
            {
                RaisePropertyChanged(nameof(BitmapLoaded));
            }
        }
    }

    private WriteableBitmap? _renderTarget;
    public WriteableBitmap? RenderTarget
    {
        get => _renderTarget;
        set => SetProperty(ref _renderTarget, value);
    }

    public bool BitmapLoaded => _bitmap != null;


    private bool _isVisiting;
    public bool IsVisiting
    {
        get => _isVisiting;
        set
        {
            if (SetProperty(ref _isVisiting, value))
            {
                //if (IsVisiting && RenderTarget != null)
                //{
                //    ClearRenderTarget();
                //}
            }
        }
    }

    private int _divider = 16;
    public int Divider
    {
        get => _divider;
        set
        {
            if (value > 0 && 96 % value == 0)
            {
                SetProperty(ref _divider, value);
            }
        }
    }

    private BitmapVisitorType _visitorType = BitmapVisitorType.RowWise;
    public BitmapVisitorType VisitorType
    {
        get => _visitorType;
        set
        {
            SetProperty(ref _visitorType, value);
        }
    }

    private int _gridCols = 1;
    public int GridCols
    {
        get => _gridCols;
        set
        {
            if (value > 0)
            {
                if (SetProperty(ref _gridCols, value))
                {
                    RequestReset();
                }
            }
        }
    }

    private int _gridRows = 1;

    public int GridRows
    {
        get => _gridRows;
        set
        {
            if (value > 0)
            {
                if (SetProperty(ref _gridRows, value))
                {
                    RequestReset();
                }
            }
        }
    }

    private ColorToCvType _pitchColorToCvType = ColorToCvType.Color;
    public ColorToCvType PitchColorToCvType
    {
        get => _pitchColorToCvType;
        set => SetProperty(ref _pitchColorToCvType, value);
    }

    private BaseColor _pitchBaseColor = BaseColor.Red;
    public BaseColor PitchBaseColor
    {
        get => _pitchBaseColor;
        set => SetProperty(ref _pitchBaseColor, value);
    }

    private ColorToCvType _velocityColorToCvType = ColorToCvType.Brightness;
    public ColorToCvType VelocityColorToCvType
    {
        get => _velocityColorToCvType;
        set => SetProperty(ref _velocityColorToCvType, value);
    }

    private BaseColor _velocityBaseColor = BaseColor.Red;
    public BaseColor VelocityBaseColor
    {
        get => _velocityBaseColor;
        set => SetProperty(ref _velocityBaseColor, value);
    }

    private ColorToCvType _pitchbendColorToCvType = ColorToCvType.Hue;
    public ColorToCvType PitchbendColorToCvType
    {
        get => _pitchbendColorToCvType;
        set => SetProperty(ref _pitchbendColorToCvType, value);
    }

    private BaseColor _pitchbendBaseColor = BaseColor.Red;
    public BaseColor PitchbendBaseColor
    {
        get => _pitchbendBaseColor;
        set => SetProperty(ref _pitchbendBaseColor, value);
    }

    private ColorToCvType _controlChangeColorToCvType = ColorToCvType.Saturation;
    public ColorToCvType ControlChangeColorToCvType
    {
        get => _controlChangeColorToCvType;
        set => SetProperty(ref _controlChangeColorToCvType, value);
    }

    private BaseColor _controlChangeBaseColor = BaseColor.Red;
    public BaseColor ControlChangeBaseColor
    {
        get => _controlChangeBaseColor;
        set => SetProperty(ref _controlChangeBaseColor, value);
    }

    private bool _legato = true;
    public bool Legato
    {
        get => _legato;
        set => SetProperty(ref _legato, value);
    }

    private bool _sendNoteOn = true;
    public bool SendNoteOn
    {
        get => _sendNoteOn;
        set => SetProperty(ref _sendNoteOn, value);
    }

    private bool _sendPitchbendOn = false;
    public bool SendPitchbendOn
    {
        get => _sendPitchbendOn;
        set => SetProperty(ref _sendPitchbendOn, value);
    }

    private bool _sendControlChangeOn = false;
    public bool SendControlChangeOn
    {
        get => _sendControlChangeOn;
        set => SetProperty(ref _sendControlChangeOn, value);
    }

    private byte _controlChangeNumber = 0;
    public byte ControlChangeNumber
    {
        get => _controlChangeNumber;
        set => SetProperty(ref _controlChangeNumber, value);
    }

    private byte _tonalRangeLower = 0;
    public byte TonalRangeLower
    {
        get => _tonalRangeLower;
        set
        {
            if (value <= _tonalRangeUpper)
            {
                SetProperty(ref _tonalRangeLower, value);
                RaisePropertyChanged(nameof(TonalRange));
            }
        }
    }

    private byte _tonalRangeUpper = 127;
    public byte TonalRangeUpper
    {
        get => _tonalRangeUpper;
        set
        {
            if (value >= _tonalRangeLower)
            {
                SetProperty(ref _tonalRangeUpper, value);
                RaisePropertyChanged(nameof(TonalRange));
            }
        }
    }

    public string TonalRange =>
        $"{((int)TonalRangeLower).ToNoteNumberString()}-{((int)TonalRangeUpper).ToNoteNumberString()}";

    private TransformSettings _transformSettings = null;
    public TransformSettings TransformSettings
    {
        get => _transformSettings;
        set => SetProperty(ref _transformSettings, value);
    }

    public ICommand ToggleSendNoteOnCommand { get; }
    public ICommand ToggleSendPitchbendOnCommand { get; }
    public ICommand ToggleSendControlChangeOnCommand { get; }
    public ICommand ToggleLegatoCommand { get; }
    public ICommand WidelRangeCommand { get; }
    public ICommand FullRangeCommand { get; }

    public ApplicationSettings(IEventAggregator aggregator)
    {
        _aggregator = aggregator;
        ToggleSendNoteOnCommand = new Prism.Commands.DelegateCommand(() =>
        {
            SendNoteOn = !SendNoteOn;
        });
        ToggleSendPitchbendOnCommand = new Prism.Commands.DelegateCommand(() =>
        {
            SendPitchbendOn = !SendPitchbendOn;
        });
        ToggleSendControlChangeOnCommand = new Prism.Commands.DelegateCommand(() =>
        {
            SendControlChangeOn = !SendControlChangeOn;
        });
        ToggleLegatoCommand = new Prism.Commands.DelegateCommand(() =>
        {
            Legato = !Legato;
        });

        WidelRangeCommand = new Prism.Commands.DelegateCommand(() =>
        {
            TonalRangeLower = 21;
            TonalRangeUpper = 108;
        });

        FullRangeCommand = new Prism.Commands.DelegateCommand(() =>
        {
            TonalRangeLower = 0;
            TonalRangeUpper = 127;
        });
    }

    public void ClearRenderTarget()
    {
        if (RenderTarget == null) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            RenderTarget.Clear();
        });
    }

    public void RequestReset()
    {
        _aggregator.GetEvent<ResetRequest>().Publish();
    }
}
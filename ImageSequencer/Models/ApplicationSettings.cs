using Egami.Imaging.Visiting;
using Prism.Mvvm;
using System.Windows.Media.Imaging;

namespace ImageSequencer.Models;

public class ApplicationSettings : BindableBase
{
    private WriteableBitmap? _bitmap;
    public WriteableBitmap? Bitmap
    {
        get => _bitmap;
        set => SetProperty(ref _bitmap, value);
    }

    private WriteableBitmap? _renderTarget;
    public WriteableBitmap? RenderTarget
    {
        get => _renderTarget;
        set => SetProperty(ref _renderTarget, value);
    }


    private bool _isVisiting;
    public bool IsVisiting
    {
        get => _isVisiting;
        set => SetProperty(ref _isVisiting, value);
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
}
using Blattwerk.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace Blattwerk.ViewModels;

public enum TrackConfigurationType
{
    Pitch,
    Velocity,
    Pitchbend,
    ControlChange
}

public sealed class TrackConfiguration : BindableBase
{
    private readonly IEventAggregator _eventAggregator;
    private readonly string _name;
    public string Name => _name;

    private int _channel = 0;

    public int Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    private bool _isVelocityTrack;

    public bool IsVelocityTrack
    {
        get => _isVelocityTrack;
        set => SetProperty(ref _isVelocityTrack, value);
    }

    private bool _isPitchTrack;

    public bool IsPitchTrack
    {
        get => _isPitchTrack;
        set => SetProperty(ref _isPitchTrack, value);
    }

    private bool _isPitchbendTrack;
    public bool IsPitchbendTrack
    {
        get => _isPitchbendTrack;
        set => SetProperty(ref _isPitchbendTrack, value);
    }

    private bool _isControlChangeTrack;
    public bool IsControlChangeTrack
    {
        get => _isControlChangeTrack;
        set => SetProperty(ref _isControlChangeTrack, value);
    }

    private readonly VelocityConfiguration _velocity = new();
    public VelocityConfiguration Velocity => _velocity;

    private PitchConfiguration _pitch = new PercussionConfiguration();
    public PitchConfiguration Pitch => _pitch;

    private readonly PitchbendConfiguration _pitchbend = new();
    public PitchbendConfiguration Pitchbend => _pitchbend;

    private readonly ControlChangeConfiguration _controlChange = new();
    public ControlChangeConfiguration ControlChange => _controlChange;

    public PitchType PitchType => (PitchType)_pitchTypeIndex;

    private int _pitchTypeIndex = 0;

    public int PitchTypeIndex
    {
        get => _pitchTypeIndex;
        set
        {
            if (SetProperty(ref _pitchTypeIndex, value))
            {
                _pitch = PitchConfigurationFactory.Create((PitchType)_pitchTypeIndex);
                RaisePropertyChanged(nameof(Pitch));
                RaisePropertyChanged(nameof(PitchType));
            }
        }
    }

    public DelegateCommand<TrackConfigurationType?> SetConfigurationCommand { get; }

    public TrackConfiguration(IEventAggregator eventAggregator, string name)
    {
        _eventAggregator = eventAggregator;
        _name = name;

        SetConfigurationCommand = new DelegateCommand<TrackConfigurationType?>(SetConfiguration);
    }

    private void SetConfiguration(TrackConfigurationType? type)
    {
        if (type == null) return;
        switch (type)
        {
            case TrackConfigurationType.Pitch:
                IsPitchTrack = true;
                _eventAggregator.GetEvent<TrackConfigurationEvent>().Publish((type.Value, _name));
                break;
            case TrackConfigurationType.Velocity:
                IsVelocityTrack = true;
                _eventAggregator.GetEvent<TrackConfigurationEvent>().Publish((type.Value, _name));
                break;
            case TrackConfigurationType.Pitchbend:
                IsPitchbendTrack = !IsPitchbendTrack;
                _eventAggregator.GetEvent<TrackConfigurationEvent>().Publish((type.Value, _name));
                break;
            case TrackConfigurationType.ControlChange:
                IsControlChangeTrack = !IsControlChangeTrack;
                break;
        }
    }
}
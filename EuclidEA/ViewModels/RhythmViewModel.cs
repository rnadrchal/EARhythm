using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using Egami.Rhythm.Pattern;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

namespace EuclidEA.ViewModels;

public class RhythmViewModel : BindableBase
{
    private readonly RhythmPattern pattern;
    private readonly IEventAggregator _eventAggregator;
    private RhythmPattern? _target = null;

    public List<StepViewModel> Steps { get; private set; }
    public List<StepViewModel>? _targetSteps = null;
    public List<StepViewModel>? TargetSteps
    {
        get => _targetSteps;
        set => SetProperty(ref _targetSteps, value);
    }

    public bool WaitingForTarget => _target == null;

    public int[] Rates => new [] { 32, 24, 16, 12, 8, 6, 4, 3, 2, 1 };
    private int _rate = 2;
    public int RateIndex
    {
        get => _rate;
        set
        {
            if (SetProperty(ref _rate, value))
            {
                RaisePropertyChanged(nameof(Rate));
            }

        }
    }

    public int Rate => Rates[RateIndex];

    public ICommand DeleteMeCommand { get; } 

    public RhythmViewModel(RhythmPattern pattern, IEventAggregator eventAggregator)
    {
        this.pattern = pattern;
        _eventAggregator = eventAggregator;
        Steps = Enumerable.Range(0, pattern.Hits.Length).Select(i => new StepViewModel
        {
            IsHit = pattern.Hits[i],
            Velocity = pattern.Velocity[i] / 4,
            Length = Math.Max(1, pattern.Lengths[i])
        }).ToList();

        DeleteMeCommand = new DelegateCommand(() => _eventAggregator.GetEvent<Events.DeleteRhythmEvent>().Publish(this));
    }

    public void SetTarget(RhythmPattern target)
    {
        _target = target;
        TargetSteps = Enumerable.Range(0, target.Hits.Length).Select(i => new StepViewModel
        {
            IsHit = target.Hits[i],
            Velocity = target.Velocity[i] / 4,
            Length = Math.Max(1, target.Lengths[i])
        }).ToList();
        RaisePropertyChanged(nameof(WaitingForTarget));
    }

}
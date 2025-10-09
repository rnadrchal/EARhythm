using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using Egami.Rhythm.Pattern;
using Prism.Mvvm;

namespace EuclidEA.ViewModels;

public class RhythmViewModel : BindableBase
{
    private readonly RhythmPattern pattern;

    public bool[] Hits => pattern.Hits;
    public int[] Velocities { get; private set; }
    public int[] Lengths { get; private set; }

    private Visibility _hitsVisibility;

    public Visibility HitsVisibility
    {
        get => _hitsVisibility;
        set => SetProperty(ref _hitsVisibility, value);
    }
    private Visibility _lengthsVisibility;

    public Visibility LengthsVisibility
    {
        get => _lengthsVisibility;
        set => SetProperty(ref _lengthsVisibility, value);
    }

    private Visibility _velocityVisibility;

    public Visibility VelocityVisibility
    {
        get => _velocityVisibility;
        set => SetProperty(ref _velocityVisibility, value);
    }

    public List<StepViewModel> Steps { get; private set; }

    public RhythmViewModel(RhythmPattern pattern)
    {
        this.pattern = pattern;
        Velocities = pattern.Velocity.Select(v => (int)(v / 10)).ToArray();
        Lengths = pattern.ToEvents().Select(e => e.Length).ToArray();
        Steps = Enumerable.Range(0, pattern.Hits.Length).Select(i => new StepViewModel
        {
            IsHit = pattern.Hits[i],
            Velocity = (int)pattern.Velocity[i] / 4,
            Length = Math.Max(1, pattern.Lengths[i])
        }).ToList();
    }


}
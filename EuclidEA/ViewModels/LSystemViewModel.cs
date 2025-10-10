using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Documents;
using Egami.Rhythm;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;

namespace EuclidEA.ViewModels;

public class LSystemViewModel : RhythmGeneratorViewModel
{
    private string _axiom;

    private int _ruleIndex = 0;

    public int RuleIndex
    {
        get => _ruleIndex;
        set
        {
            if (SetProperty(ref _ruleIndex, value))
            {
                RaisePropertyChanged(nameof(Rules));
                RaisePropertyChanged(nameof(RuleCharacters));
                HitSymbolIndex = 0;
                RaisePropertyChanged(nameof(HitSymbol));
            }
        }
    }

    public Dictionary<char, string> Rules => LSystemGenerator.Rules[_ruleIndex];

    public int NumberOfRules => LSystemGenerator.Rules.Count;

    private int _iterations = 3;
    public int Iterations
    {
        get => _iterations;
        set => SetProperty(ref _iterations, value);
    }

    public char[] RuleCharacters => GetRuleCharacters();

    private int _hitSymbolIndex = 0;
    public int HitSymbolIndex
    {
        get => _hitSymbolIndex;
        set
        {
            if (SetProperty(ref _hitSymbolIndex, value))
            {
                RaisePropertyChanged(nameof(HitSymbol));
            }
        }
    }

    public char HitSymbol => GetRuleCharacters().ElementAtOrDefault(_hitSymbolIndex);

    protected override IRhythmGenerator Generator => new LSystemGenerator(_axiom, Rules, 10, 'A');
    public override string Name => "L-System Genrator";

    protected override RhythmPattern Generate(RhythmContext context)
    {
        _axiom = GetRuleCharacters().First().ToString();
        return base.Generate(context);
    }

    private char[] GetRuleCharacters() => Rules.Values.Select(v => v.ToCharArray()).SelectMany(c => c).Distinct().OrderBy(c => c).ToArray();
}
using System.Collections.Generic;
using System.Linq;
using Egami.Rhythm;
using Egami.Rhythm.Generation;
using Egami.Rhythm.Pattern;

namespace EuclidEA.ViewModels.Rhythm;

public class LSystemViewModel : RhythmGeneratorViewModel
{
    private static string[] RuleNames = new[]
    {
        "FIBO",
        "COND",
        "EXPA",
        "CLST",
        "SYMM",
        "CHAO",
        "FRAC",
        "BINR",
        "POLY"
    };

    public int MaximumRuleCharacter => RuleCharacters.Length - 1;
    private int _axiomIndex;

    public int AxiomIndex
    {
        get => _axiomIndex;
        set
        {
            if (SetProperty(ref _axiomIndex, value))
            {
                RaisePropertyChanged(nameof(Axiom));
            }
        }
    }

    public string Axiom => RuleCharacters[_axiomIndex].ToString();


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
                RaisePropertyChanged(nameof(RuleName));
                RaisePropertyChanged(nameof(RuleCharacters));
                RaisePropertyChanged(nameof(RuleString));
                RaisePropertyChanged(nameof(MaximumRuleCharacter));
            }
        }
    }

    public string RuleName => RuleNames[_ruleIndex];

    public Dictionary<char, string> Rules => LSystemGenerator.Rules[_ruleIndex];

    public int NumberOfRules => LSystemGenerator.Rules.Count;

    private int _iterations = 3;
    public int Iterations
    {
        get => _iterations;
        set => SetProperty(ref _iterations, value);
    }

    public char[] RuleCharacters => GetRuleCharacters();

    public string RuleString => string.Join("", RuleCharacters.Select(c => c.ToString()));

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

    protected override IRhythmGenerator Generator => new LSystemGenerator(Axiom, Rules, Iterations, HitSymbol);
    public override string Name => "L-System";

    protected override RhythmPattern Generate(RhythmContext context)
    {
        return Generator.Generate(context);
    }

    private char[] GetRuleCharacters() => Rules.Keys.ToArray();
}
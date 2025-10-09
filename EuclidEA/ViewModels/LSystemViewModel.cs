using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Windows.Documents;
using Egami.Rhythm.Generation;

namespace EuclidEA.ViewModels;

public class LSystemViewModel : RhythmGeneratorViewModel
{
    private List<Dictionary<char, string>> _availableRules = new()
    {
        // Periodisch
        new()
        {
            { 'A', "AB" },
            { 'B', "A" }
        },
        // Alternierend
        new()
        {
            { 'X', "XY" },
            {'Y', "X" }
        },
        // Verdichtend
        new()
        {
            { 'A', "AA" },
            { 'B', "B"}
        },
        // Expandierend
        new()
        {
            { 'A', "AB" },
            { 'B', "BB" }
        }
    };

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
            }
        }
    }

    public Dictionary<char, string> Rules => _availableRules[_ruleIndex];

    public string Axiom
    {
        get => _axiom;
        set => SetProperty(ref _axiom, value);
    }
    protected override IRhythmGenerator Generator => new LSystemGenerator(_axiom, Rules, 10, 'A');
    public override string Name => "L-System Genrator";
}
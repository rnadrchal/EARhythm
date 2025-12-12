using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace Egami.Sequencer.Automation;

public sealed class CcBinding
{
    public int ControllerNumber { get; }
    public FourBitNumber? ChannelFilter { get; }
    public AutomationParameter Parameter { get; }

    public CcBinding(
        int controllerNumber,
        AutomationParameter parameter,
        FourBitNumber? channelFilter = null)
    {
        if (controllerNumber is < 0 or > 127)
            throw new ArgumentOutOfRangeException(nameof(controllerNumber));

        ControllerNumber = controllerNumber;
        Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        ChannelFilter = channelFilter;
    }

    public bool Matches(ControlChangeEvent cc)
    {
        if (cc.ControlNumber != (SevenBitNumber)ControllerNumber)
            return false;

        if (ChannelFilter.HasValue && cc.Channel != ChannelFilter.Value)
            return false;

        return true;
    }
}
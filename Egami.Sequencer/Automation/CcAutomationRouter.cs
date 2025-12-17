using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Egami.Sequencer.Automation;

public class CcAutomationRouter : IDisposable
{
    private readonly InputDevice _inputDevice;
    private readonly List<CcBinding> _bindings = new();
    private readonly object _sync = new();

    public CcAutomationRouter(InputDevice inputDevice)
    {
        _inputDevice = inputDevice ?? throw new ArgumentNullException(nameof(inputDevice));
        _inputDevice.EventReceived += OnEventReceived;
    }

    public void AddBinding(CcBinding binding)
    {
        if (binding == null) throw new ArgumentNullException(nameof(binding));
        lock (_sync)
        {
            _bindings.Add(binding);
        }
    }

    public void RemoveBinding(CcBinding binding)
    {
        if (binding == null) return;
        lock (_sync)
        {
            _bindings.Remove(binding);
        }
    }

    private void OnEventReceived(object? sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is not ControlChangeEvent cc)
            return;

        List<CcBinding> bindingsSnapshot;

        lock (_sync)
        {
            if (_bindings.Count == 0)
                return;

            bindingsSnapshot = new List<CcBinding>(_bindings);
        }

        foreach (var binding in bindingsSnapshot)
        {
            if (!binding.Matches(cc))
                continue;

            binding.Parameter.SetFromCc(cc.ControlValue);
        }
    }

    public void Dispose()
    {
        _inputDevice.EventReceived -= OnEventReceived;
    }
}
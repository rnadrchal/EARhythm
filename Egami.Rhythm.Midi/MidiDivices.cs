using Melanchall.DryWetMidi.Multimedia;

namespace Egami.Rhythm.Midi;

public static class MidiDevices
{
    public static InputDevice Input { get; private set; }
    public static OutputDevice Output { get; private set; }

    public static void Initialize(string deviceName)
    {
        Input = InputDevice.GetByName(deviceName);
        Output = OutputDevice.GetByName(deviceName);

        Input?.StartEventsListening();
    }

    public static void Initialize(string clockDeviceName, string outputDeviceName)
    {
        Input = InputDevice.GetByName(clockDeviceName);
        Output = OutputDevice.GetByName(outputDeviceName);
        Input?.StartEventsListening();
    }
}
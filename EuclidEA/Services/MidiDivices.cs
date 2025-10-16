using Melanchall.DryWetMidi.Multimedia;

namespace EuclidEA.Services;

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
}
using Egami.Rhythm.Midi;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Prism.Events;

namespace EuclidEA.Services;

public class ClockEvent : PubSubEvent<ulong>
{
}

public class BeatEvent : PubSubEvent
{

}

public class OffBeatEvent : PubSubEvent
{

}

public class StopEvent : PubSubEvent
{
}

public class MidiClock
{

    private IEventAggregator _eventAggregator;

    private int _divider = 16;

    private ulong _clockCount = 0;

    public MidiClock(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;

        MidiDevices.Input.EventReceived += OnEventReceived;
    }

    private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
    {

        if (e.Event is StartEvent)
        {
            _clockCount = 0;
        }

        if (e.Event is StopEvent)
        {
            _eventAggregator.GetEvent<StopEvent>().Publish();
        }

        if (e.Event is TimingClockEvent clockEvent)
        {
            _eventAggregator.GetEvent<ClockEvent>().Publish(_clockCount);

            if (_clockCount % 24 == 0)
            {
                _eventAggregator.GetEvent<BeatEvent>().Publish();
            }

            if (_clockCount % 24 == 12)
            {
                _eventAggregator.GetEvent<OffBeatEvent>().Publish();
            }
            _clockCount++;
        }
    }
}
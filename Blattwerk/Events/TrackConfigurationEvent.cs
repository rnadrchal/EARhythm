using Blattwerk.ViewModels;
using Prism.Events;

namespace Blattwerk.Events;


class TrackConfigurationEvent : PubSubEvent<(TrackConfigurationType type, string key)>
{
}
using System.Windows.Input;
using Prism.Events;

namespace StepMutator.Events;

public record GlobalKeyPayload(Key Key, bool IsDown, ModifierKeys Modifiers, KeyEventArgs Args);

public class GlobalKeyEvent : PubSubEvent<GlobalKeyPayload> { }
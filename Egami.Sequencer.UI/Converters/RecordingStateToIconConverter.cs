using System.Globalization;
using System.Windows.Data;
using Material.Icons;

namespace Egami.Sequencer.UI.Converters;

public class RecordingStateToIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isRecording)
        {
            return isRecording ? MaterialIconKind.Pause : MaterialIconKind.Record;
        }
        return MaterialIconKind.Record;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
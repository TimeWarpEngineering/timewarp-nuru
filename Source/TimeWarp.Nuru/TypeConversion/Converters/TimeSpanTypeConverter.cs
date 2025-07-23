namespace TimeWarp.Nuru.TypeConversion.Converters;

/// <summary>
/// Converts string values to TimeSpan.
/// Supports formats like "1:30:00", "00:00:30", "1.05:00:00".
/// </summary>
public class TimeSpanTypeConverter : IRouteTypeConverter
{
    public Type TargetType => typeof(TimeSpan);
    public string ConstraintName => "timespan";

    public bool TryConvert(string value, out object? result)
    {
        if (TimeSpan.TryParse(value, out TimeSpan timeSpanValue))
        {
            result = timeSpanValue;
            return true;
        }

        result = null;
        return false;
    }
}
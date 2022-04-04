using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Serilog.Benya.Formatter;

/// <summary>
/// An <see cref="ITextFormatter"/> that writes events in a compact JSON format, for consumption in environments 
/// without message template support. Message templates are rendered into text and a hashed event id is included.
/// </summary>
public class MyTestFormatterJson : ITextFormatter
{
    readonly JsonValueFormatter _valueFormatter;

    /// <summary>
    /// Construct a <see cref="CompactJsonFormatter"/>, optionally supplying a formatter for
    /// <see cref="LogEventPropertyValue"/>s on the event.
    /// </summary>
    /// <param name="valueFormatter">A value formatter, or null.</param>
    public MyTestFormatterJson(JsonValueFormatter valueFormatter = null)
    {
        _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
    }

    /// <summary>
    /// Format the log event into the output. Subsequent events will be newline-delimited.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    public void Format(LogEvent logEvent, TextWriter output)
    {
        FormatEvent(logEvent, output, _valueFormatter);
        output.WriteLine();
    }

    /// <summary>
    /// Format the log event into the output.
    /// </summary>
    /// <param name="logEvent">The event to format.</param>
    /// <param name="output">The output.</param>
    /// <param name="valueFormatter">A value formatter for <see cref="LogEventPropertyValue"/>s on the event.</param>
    public static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
        if (output == null) throw new ArgumentNullException(nameof(output));
        if (valueFormatter == null) throw new ArgumentNullException(nameof(valueFormatter));

        output.Write("{\"timeSpan\":\"");
        output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
        output.Write("\",\"message\":");
        var message = logEvent.MessageTemplate.Render(logEvent.Properties);
        JsonValueFormatter.WriteQuotedJsonString(message, output);

        output.Write(",\"eventId\":\"");
        var id = logEvent.MessageTemplate.Text.GetHashCode();   /*EventIdHash.Compute(logEvent.MessageTemplate.Text);*/
        output.Write(id.ToString("x8"));
        output.Write('"');

        output.Write(",\"level\":\"");
        output.Write(logEvent.Level);
        output.Write('\"');


        if (logEvent.Exception != null)
        {
            output.Write(",\"exception\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        foreach (var property in logEvent.Properties)
        {
            var name = property.Key;
            if (name.Length > 0 && name[0] == '@')
            {
                // Escape first '@' by doubling
                name = '@' + name;
            }

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        output.Write('}');
    }
}
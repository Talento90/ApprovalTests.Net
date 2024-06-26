using ApprovalUtilities.CallStack;
using ApprovalUtilities.Persistence;
using ApprovalUtilities.SimpleLogger.Writers;
using ApprovalUtilities.Utilities;

namespace ApprovalUtilities.SimpleLogger;

public class LoggerInstance
{
    public IAppendable Writer = new MultiWriter(new ConsoleWriter(), new DebuggerWriter());
    int indent;
    public int TabSize = 4;
    bool showMarkerIn = true;
    bool showVariables = true;
    bool showEvents = true;
    bool showSql = true;
    bool showTimestamp = true;
    bool showTimeDifference = true;
    ILoader<DateTime> clock = new Clock();
    DateTime lastTime = DateTime.Now;

    public StringBuilderLogger LogToStringBuilder()
    {
        Show(markerIn: true, variables: true, events: true, sql: true, timestamp: false, timeDifference: false);
        var log = new StringBuilderLogger();
        Writer = log;
        return log;
    }

    public void MarkerIn() =>
        drawMarker("=> ", true);

    public void MarkerOut() =>
        drawMarker("<= ", false);

    void drawMarker(string arrow, bool stepIn)
    {
        if (!showMarkerIn)
        {
            return;
        }

        var method = GetCallingMethod();
        indent -= stepIn ? 0 : TabSize;
        Write(arrow + method);
        indent += stepIn ? TabSize : 0;
    }

    public string GetCallingMethod()
    {
        var outsideCallingMethod =
            new Caller().Methods.First(m => m.DeclaringType.Namespace != GetType().Namespace);
        return outsideCallingMethod.ToStandardString();
    }

    void Write(string text)
    {
        var time = showTimestamp ? clock.Load() + " " : "";
        var difference = "";
        if (showTimeDifference)
        {
            var t = clock.Load();
            var diff = t - lastTime;
            lastTime = t;
            difference = $"~{diff.TotalMilliseconds:000000}ms ";
        }

        var message = text.Replace(Environment.NewLine, Environment.NewLine + "\t");
        Writer.AppendLine(time + difference + GetIndentation() + message);
    }

    string GetIndentation() =>
        "".PadLeft(indent, ' ');

    public string Event(string message, params object[] items)
    {
        if (showEvents)
        {
            Write(string.Format("Event: " + message, items));
        }
        return message;
    }

    public string Message(string message)
    {
        if (showEvents)
        {
            Write("Message: " + message);
        }
        return message;
    }

    public void Variable(string name, object value)
    {
        if (showVariables)
        {
            Write("Variable: " + name + " = '" + value + "'");
        }
    }

    public string Sql(string sql)
    {
        if (showSql)
        {
            Write("Sql: " + sql);
        }
        return sql;
    }

    public string Miscellaneous(string label, string message)
    {
        Write($"{label}: {message}");
        return message;
    }

    public void Warning(Exception except, params string[] additional) =>
        Writer.AppendLine(except.FormatError(additional));

    public string Warning(string format, params object[] data)
    {
        PrintWarning(string.Format(format, data));
        return string.Format(format, [data]);
    }

    void PrintWarning(params string[] lines) =>
        Writer.AppendLine(ExceptionUtilities.FormatAsError(lines));

    public void Show(bool markerIn = true, bool variables = true, bool events = true, bool sql = true,
        bool timestamp = true, bool timeDifference = true)
    {
        showMarkerIn = markerIn;
        showVariables = variables;
        showEvents = events;
        showSql = sql;
        showTimestamp = timestamp;
        showTimeDifference = timeDifference;
    }

    public void UseTimer(ILoader<DateTime> timeLoader)
    {
        lastTime = timeLoader.Load();
        clock = timeLoader;
    }
}
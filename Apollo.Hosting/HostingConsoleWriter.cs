using System.Text;

namespace Apollo.Hosting;

public class HostingConsoleWriter : TextWriter
{
    private readonly StringBuilder _buffer = new();
    private readonly Action<string> _logCallback;

    public HostingConsoleWriter(Action<string> logCallback)
    {
        _logCallback = logCallback;
    }

    public override void WriteLine(string? value)
    {
        if (value != null)
        {
            _logCallback(value);
        }
    }

    public override void Write(char value)
    {
        if (value == '\n')
        {
            var line = _buffer.ToString();
            if (!string.IsNullOrEmpty(line))
            {
                _logCallback(line);
            }
            _buffer.Clear();
        }
        else
        {
            _buffer.Append(value);
        }
    }

    public override Encoding Encoding => Encoding.UTF8;
} 
using System.Collections;
using System.Collections.ObjectModel;

namespace BlobHandler;

public class EnvironmentVariableManager : IReadOnlyDictionary<string, string>
{
    private ReadOnlyDictionary<string, string> _environmentVariables;

    public IReadOnlyDictionary<string, string> EnvironmentVariables => _environmentVariables;

    public EnvironmentVariableManager(IEnumerable<string> requiredVariables)
    {
        var missingVariables = new List<string>();
        var environmentVariables = new Dictionary<string, string>();

        foreach (var variable in requiredVariables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrEmpty(value))
            {
                missingVariables.Add(variable);
            }
            else
            {
                environmentVariables[variable] = value;
            }
        }

        if (missingVariables.Count > 0)
        {
            throw new ArgumentException($"The following environment variables are not defined: {string.Join(", ", missingVariables)}");
        }

        _environmentVariables = new ReadOnlyDictionary<string, string>(environmentVariables);
    }

    public string this[string key] => _environmentVariables[key];

    public IEnumerable<string> Keys => _environmentVariables.Keys;
    public IEnumerable<string> Values => _environmentVariables.Values;
    public int Count => _environmentVariables.Count;
    public bool ContainsKey(string key) => _environmentVariables.ContainsKey(key);
    public bool TryGetValue(string key, out string value) => _environmentVariables.TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _environmentVariables.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _environmentVariables.GetEnumerator();
}

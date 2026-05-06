using System.Text;
using UnityEngine;

public abstract class EggNamedDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string _id;

    public string Id => _id;

    protected virtual void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(_id))
            _id = BuildIdFromName(name);
    }

    protected virtual void OnValidate()
    {
        _id = BuildIdFromName(name);
    }

    protected static string BuildIdFromName(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return string.Empty;

        StringBuilder builder = new();
        string trimmed = source.Trim();
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = char.ToLowerInvariant(trimmed[i]);
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                builder.Append(c);
                continue;
            }

            if (builder.Length > 0 && builder[builder.Length - 1] != '_')
                builder.Append('_');
        }

        return builder.ToString().Trim('_');
    }
}

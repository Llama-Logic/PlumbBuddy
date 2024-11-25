namespace PlumbBuddy.Services;

public class MarkupLocalizer :
    IMarkupLocalizer
{
    public MarkupLocalizer(IStringLocalizer stringLocalizer)
    {
        ArgumentNullException.ThrowIfNull(stringLocalizer);
        this.stringLocalizer = stringLocalizer;
    }

    readonly IStringLocalizer stringLocalizer;

    public MarkupString this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return (MarkupString)stringLocalizer[name].Value;
        }
    }

    public MarkupString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return (MarkupString)stringLocalizer[name, arguments].Value;
        }
    }

    public IEnumerable<MarkupString> GetAllStrings(bool includeParentCultures) =>
        stringLocalizer.GetAllStrings(includeParentCultures)
            .Select(ls => (MarkupString)ls.Value);
}

public class MarkupLocalizer<T>(IStringLocalizer<T> stringLocalizer) :
    MarkupLocalizer(stringLocalizer),
    IMarkupLocalizer<T>
{
}

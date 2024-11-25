namespace PlumbBuddy.Services;

public interface IMarkupLocalizer
{
    MarkupString this[string name] { get; }

    MarkupString this[string name, params object[] arguments] { get; }

    IEnumerable<MarkupString> GetAllStrings(bool includeParentCultures);
}

public interface IMarkupLocalizer<out T> :
    IMarkupLocalizer
{
}

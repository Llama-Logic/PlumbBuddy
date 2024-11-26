namespace PlumbBuddy.Services;

public interface IMarkupLocalizer
{
    StepperLocalizedStrings StepperLocalizedStrings =>
        new()
        {
            Completed = AppText.Common_Completed,
            Finish = AppText.Common_Finish,
            Next = AppText.Common_Next,
            Optional = AppText.Common_Optional,
            Previous = AppText.Common_Previous,
            Skip = AppText.Common_Skip,
            Skipped = AppText.Common_Skipped
        };

    MarkupString this[string name] { get; }

    MarkupString this[string name, params object[] arguments] { get; }

    IEnumerable<MarkupString> GetAllStrings(bool includeParentCultures);
}

public interface IMarkupLocalizer<out T> :
    IMarkupLocalizer
{
}

namespace PlumbBuddy;

static partial class Extensions
{
    public static string ToLocalizedQuantity(this string input, int quantity, ShowQuantityAs showQuantityAs = ShowQuantityAs.Numeric)
    {
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is not "tr")
            return input.ToQuantity(quantity, showQuantityAs);

        // Turkish nouns stay singular after a quantity; Humanizer's inflector uses English rules.
        return showQuantityAs switch
        {
            ShowQuantityAs.None => input,
            ShowQuantityAs.Words => $"{quantity.ToWords()} {input}",
            _ => $"{quantity} {input}"
        };
    }
}
using Foundation;
using UIKit;

namespace PlumbBuddy.Platforms.MacCatalyst;

public static class FileDragAndDrop
{
    public static void AddHandler(UIView view, Func<IReadOnlyList<string>, Task> dropHandler)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(dropHandler);
        view.AddInteraction(new UIDropInteraction(new DropInteractionDelegate() { Content = content }));
    }

    public static void RemoveHandler(UIView view, Func<IReadOnlyList<string>, Task> dropHandler)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(dropHandler);
        foreach (var interaction in view.Interactions
            .OfType<UIDropInteraction>()
            .Where(did => did.Handler == dropHandler))
            view.RemoveInteraction(interaction);
    }
}

class DropInteractionDelegate : UIDropInteractionDelegate
{
    public Func<IReadOnlyList<string>, Task> Handler { get; init; }

    public override UIDropProposal SessionDidUpdate(UIDropInteraction interaction, IUIDropSession session) =>
        new UIDropProposal(UIDropOperation.Copy);

    public override void PerformDrop(UIDropInteraction interaction, IUIDropSession session)
    {
        var paths = new List<string>();
        foreach (var item in session.Items)
            item.ItemProvider.LoadItem(UniformTypeIdentifiers.UTTypes.Json.Identifier, null, async (data, error) =>
            {
                if (data is NSUrl nsData
                    && !string.IsNullOrWhiteSpace(nsData.Path))
                    paths.Add(nsData.Path);
            });
        await Handler.Invoke(paths.ToImmutableArray());
    }
}
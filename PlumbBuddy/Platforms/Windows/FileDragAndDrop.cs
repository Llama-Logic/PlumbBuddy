using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using DataPackageOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;
using DragEventArgs = Microsoft.UI.Xaml.DragEventArgs;

namespace PlumbBuddy.Platforms.Windows;

public static class FileDragAndDrop
{
    static readonly Dictionary<UIElement, List<Func<IReadOnlyList<string>, Task>>> dropHandlersByUiElement = [];

    public static void AddHandler(UIElement element, Func<IReadOnlyList<string>, Task> dropHandler)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(dropHandler);
        ref var dropHandlers = ref CollectionsMarshal.GetValueRefOrAddDefault(dropHandlersByUiElement, element, out var exists)!;
        if (!exists)
        {
            dropHandlers = [];
            element.AllowDrop = true;
            element.DragOver += HandleDragOver;
            element.Drop += HandleDrop;
        }
        dropHandlers.Add(dropHandler);
    }

    static void HandleDragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy
            : DataPackageOperation.None;
    }

    static async void HandleDrop(object sender, DragEventArgs e)
    {
        if (sender is UIElement element
            && dropHandlersByUiElement.TryGetValue(element, out var dropHandlers)
            && e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var paths = (await e.DataView.GetStorageItemsAsync())
                .OfType<IStorageItem>()
                .Select(file => file.Path)
                .ToImmutableArray();
            foreach (var dropHandler in dropHandlers)
                await dropHandler.Invoke(paths);
        }
    }

    public static void RemoveHandler(UIElement element, Func<IReadOnlyList<string>, Task> dropHandler)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(dropHandler);
        if (dropHandlersByUiElement.TryGetValue(element, out var dropHandlers))
        {
            if (dropHandlers.Remove(dropHandler)
                && dropHandlers.Count is 0)
            {
                element.AllowDrop = false;
                element.DragOver -= HandleDragOver;
                element.Drop -= HandleDrop;
                dropHandlersByUiElement.Remove(element);
            }
        }
    }
}

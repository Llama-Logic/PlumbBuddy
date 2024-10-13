namespace PlumbBuddy.Components.Controls;

partial class InlineMarkdown
{
    string markdown = string.Empty;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [SuppressMessage("Usage", "BL0006: Do not use RenderTree types", Justification = "Yeah well, this is what you're doing now, so it's what I'm doing now.")]
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && ChildContent is { } childContent)
        {
            using var builder = new RenderTreeBuilder();
            childContent(builder);
            using var writer = new StringWriter();
            var renderTree = builder.GetFrames();
            foreach (var frame in renderTree.Array)
            {
                if (frame.FrameType is RenderTreeFrameType.Markup)
                    writer.Write(frame.MarkupContent);
                else if (frame.FrameType is RenderTreeFrameType.Text)
                    writer.Write(frame.TextContent);
            }
            var lines = writer.ToString().Split(Environment.NewLine).ToList();
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
                lines.RemoveAt(lines.Count - 1);
            if (lines.Count > 0)
            {
                var leastCommonPadding = lines.Select(line => line.TakeWhile(c => c is ' ').Count()).Min();
                var unpaddedLines = lines.Select(line => line[leastCommonPadding..]);
                markdown = string.Join(Environment.NewLine, unpaddedLines);
                StateHasChanged();
            }
        }
    }
}

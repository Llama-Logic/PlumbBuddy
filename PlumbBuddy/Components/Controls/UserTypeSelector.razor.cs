namespace PlumbBuddy.Components.Controls;

partial class UserTypeSelector
{
    [Parameter]
    public UserType Type { get; set; }

    [Parameter]
    public EventCallback<UserType> TypeChanged { get; set; }

    int UserTypeSelectedIndex
    {
        get => (int)Type;
        set
        {
            Type = (UserType)value;
            TypeChanged.InvokeAsync(Type);
        }
    }
}

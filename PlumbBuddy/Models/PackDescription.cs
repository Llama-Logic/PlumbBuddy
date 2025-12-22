namespace PlumbBuddy.Models;

public class PackDescription
{
    public required string EnglishName { get; set; }
    public string? EaPromoCode { get; set; }
    public required string EaStub { get; set; }
    public bool IsCreatorContent { get; set; }
    public PackDescriptionKitType? KitType { get; set; }
    public decimal RetailUsd { get; set; }
    public required string SteamStub { get; set; }
    public PackDescriptionSubType? SubType { get; set; }
    public PackDescriptionType Type { get; set; }
}

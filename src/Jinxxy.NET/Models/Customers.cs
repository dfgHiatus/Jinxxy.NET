namespace Jinxxy.Models;

public sealed record Customer
{
    public string UserId { get; init; } = "";
    public string Object { get; init; } = "";
    public decimal LoyaltyAmount { get; init; }
    public JinxxyUser User { get; init; } = new();
}

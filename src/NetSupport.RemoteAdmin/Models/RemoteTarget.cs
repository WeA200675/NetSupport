namespace NetSupport.RemoteAdmin.Models;

public sealed class RemoteTarget
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? Description { get; set; }

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Host : $"{Name} ({Host})";
}

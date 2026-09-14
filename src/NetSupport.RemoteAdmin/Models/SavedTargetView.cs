namespace NetSupport.RemoteAdmin.Models;

/// <summary>
/// Persisted combination of target-list filters for recurring admin workflows.
/// </summary>
public sealed class SavedTargetView
{
    public string Name { get; set; } = string.Empty;
    public string? SearchText { get; set; }
    public string? Group { get; set; }
    public bool FavoritesOnly { get; set; }

    public override string ToString() => Name;
}

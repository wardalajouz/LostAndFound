namespace LostAndFound.Api.Contracts.Admin;

/// <summary>Admin deletion is soft, so a deleted post can be restored by setting it back to Visible.</summary>
public enum PostVisibility { Visible, Hidden, Deleted }

using TL;

namespace FolderUIGram;

public record ChannelModel(long Id, string Title, ChatBase Chat)
{
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? $"[Unknown/Hidden] ({Id})" : $"{Title} ({Chat.GetType().Name})";
}
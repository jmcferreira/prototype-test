using System;
using System.Collections.Generic;

/// <summary>
/// Static battle log that collects game events for display in the UI.
/// Resolvers, units, and turn logic call Add* methods to record entries.
/// </summary>
public static class BattleLog
{
    public enum EntryType { TurnHeader, CardName, Action }

    public struct Entry
    {
        public EntryType Type;
        public string Text;
    }

    private static readonly List<Entry> _entries = new();

    /// <summary>Fired when a new entry is added.</summary>
    public static event Action<Entry> OnEntryAdded;

    public static IReadOnlyList<Entry> Entries => _entries;

    /// <summary>
    /// Clears all entries and subscribers. Call at start of each game session
    /// to handle static state persisting across Unity editor play sessions.
    /// </summary>
    public static void Clear()
    {
        _entries.Clear();
        OnEntryAdded = null;
    }

    public static void AddTurnHeader(string unitName, int turnNumber)
    {
        Add(EntryType.TurnHeader, $"{unitName} - Turn {turnNumber}");
    }

    public static void AddCardName(string cardName)
    {
        Add(EntryType.CardName, cardName);
    }

    public static void AddAction(string text)
    {
        Add(EntryType.Action, $"- {text}");
    }

    private static void Add(EntryType type, string text)
    {
        var entry = new Entry { Type = type, Text = text };
        _entries.Add(entry);
        OnEntryAdded?.Invoke(entry);
    }
}

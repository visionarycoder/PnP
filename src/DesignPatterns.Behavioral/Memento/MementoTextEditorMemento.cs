namespace Snippets.DesignPatterns.Behavioral.Memento
{
    // ============================================================================
    // MEMENTO PATTERN IMPLEMENTATION
    // ============================================================================

    // ============================================================================
    // TEXT EDITOR EXAMPLE
    // ============================================================================

    /// <summary>
    /// Text editor memento storing complete editor state
    /// </summary>
    public class MementoTextEditorMemento(
        string content,
        int cursorPosition,
        string fontName,
        int fontSize,
        bool isBold,
        bool isItalic,
        string description)
        : IMemento
    {
        public string Content { get; } = content;
        public int CursorPosition { get; } = cursorPosition;
        public string FontName { get; } = fontName;
        public int FontSize { get; } = fontSize;
        public bool IsBold { get; } = isBold;
        public bool IsItalic { get; } = isItalic;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string Description { get; } = description;
    }

    // ============================================================================
    // GAME STATE EXAMPLE
    // ============================================================================

    // ============================================================================
    // GENERIC CARETAKER IMPLEMENTATION
    // ============================================================================

    // ============================================================================
    // ADVANCED MEMENTO FEATURES
    // ============================================================================
}
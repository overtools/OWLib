#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using static TankLib.Helpers.Logger;

namespace DataTool.Helper;

public class ScopedSpellCheck {
    // storing a billion strings for unlocks isn't great but no other way...
    // symspell has a staging system, but it requires creating a SymSpell up-front which is expensive
    private readonly HashSet<string> m_values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private SymSpell? m_symSpell;

    public void Add(string value) {
        if (value == "*") return;
        if (ulong.TryParse(value, NumberStyles.HexNumber, null, out _)) return; // don't correct guids
        
        m_values.Add(value);
    }

    private void CommitSpellCheck() {
        if (m_symSpell != null) return;
        
        m_symSpell = new SymSpell(m_values.Count, 6);
        foreach (var h in m_values) {
            m_symSpell.CreateDictionaryEntry(h, 1);
        }
    }

    public string? TryGetSuggestion(string? text) {
        CommitSpellCheck();

        if (text == null || text == "*") {
            // nothing to check
            return null;
        }

        var correctedStr = m_symSpell!.Lookup(text, SymSpell.Verbosity.Closest);
        if (correctedStr.Count == 0 || correctedStr[0].term == text) {
            // no useful suggestions
            return null;
        }

        return correctedStr[0].term;
    }

    public void LogSpellCheck(string? text) {
        var suggestion = TryGetSuggestion(text);
        if (suggestion == null) return;

        Warn("SpellCheck", $"Did you mean \"{suggestion}\"?");
    }
}
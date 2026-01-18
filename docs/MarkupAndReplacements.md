# Markup

Some advanced features are accessible via tags, denoted by square brackets (`[` and `]`).

A tag can denote:
- a **context change**, if it is empty or single word (no space separator inside);
- **inline content** that is not plain text, if it has multiple words (space separated):
  - `[ipa {text}]` - Phonetic spelling
  - `[snd {name}]` or `[audio {name}]` - Sound effect
  - `[wait {time}]` or `[break {time}]` - Adds a pause of a specified duration

Tags fulfilling any of the following are invalid and are skipped over.

- Tag has nested tags.
- Tag is not closed.
- Tag has multiple words and does not match inline content.

## Contexts

A context is a named discriminator for text replacements and voice changes.

A single context is kept track of when parsing the message. 
The context starts out empty and is changed at every context tag. 
Contexts are not be nested, new context completely overwrites the old one.

Context names are trimmed and are case sensitive.

### Text Replacements

Portions of written text can be automatically replaced. This allows macros and pronunciation corrections.

The pattern that dictates replacement is written as a regex.
Each pattern is applied to text that is between tags and message bounds. A pattern never matches tags.
Patterns are applied in order they are placed in the replacement table.

Text replacements are only applied inside the context specified. 
If an `*` is specified as context, the replacement is applied in all contexts.
Empty context may have text replacements.

The replacement text may contain tags.

### Voice Changes

A specific voice can be set for a specific context.
That voice will be used inside that context.

Voice changes have priority in the order they are listed in the table.
Voice changes for empty contexts are ignored.

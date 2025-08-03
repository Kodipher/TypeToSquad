# Markup

Some advanced features are accessible via tags, denoted by square brackets (`[` and `]`).

A tag can denote:
- a **voice change**, if it matches a hint in voice changes table;
- a text **replacement context change**, if it does not match a voice change hint;
- **inline content** that is not plain text, if its any of the following:
  - `[ipa {text}]` - Phonetic spelling
  - `[snd {name}]` or `[audio {name}]` - Sound effect


Tags fulfilling any of the following are invalid. All invalid tags are skipped over.
- Tag has nested tags.
- Tag is not closed.
- Tag has multiple words and does not match inline content.

Empty tags are valid, they are context replacements with empty context. Voice change hints must not be empty.
Voice and context change tags must only have 1 word, while inline content tags have multiple.
Voice changes have priority over context changes if both match.


# Text Replacements

Some of submitted text can be automatically replaced. This allows macros and pronunciation corrections.
Text replacement do not apply to tags but may contain tags.

The pattern that dictates replacement is written as a regex.
Each pattern is applied to text that is between tags and message bounds (A pattern never matches tags).
Patterns are applied in order they are placed in the replacement table.

Text replacements are only applied in the context specified. 
The context starts out empty and is changed at every context tag.
If a `*` is specified as a replacement context it is valid for all contexts.

The replacement text may contain tags.


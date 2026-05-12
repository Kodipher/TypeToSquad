# Markup

Some advanced features are accessible via tags.

Instead of being plain text, a tag represents
some inline content (e.g. `[audio foo]`) or
a running change (e.g. `[voice de]`).

Additionally, there is a special empty tag `[]` that
clears any running changes set by other tags.

A tag is composed of a type and a possible argument (value), separated by a space,
e.g. `[wait 0.5s]` is a tag of type `wait` with a value of `0.5s`.

A tag with no explicit argument, like `[foo]`, has an empty string as its value.

Invalid syntax (nested tags, unclosed tags, unknown tags) is skipped over.


## Built-in tags

A few tags are built:
- `[ipa {text}]` - Explicit phonetic spelling
- `[voice {hint}]` - A voice change, running change.
- `[audio {hint}]` - A sound effect
- `[wait {time}]` or `[break {time}]` - A pause of a specified duration


### Voice and Audio Hints

Voice changes and sound effects require a bit of setup.

To avoid typing out long file paths and voice names,
the tags have hints as their argument. The actual 
sounds and voices are set in the respective tables.

Example: `[audio buzzer]` will play a sound effect
that has the hint `buzzer` in the sound effect table.

Empty hints have special handling:
- `[audio]` does nothing
- `[voice]` resets the voice to default.

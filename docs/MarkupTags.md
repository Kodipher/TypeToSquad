# Markup

Some advanced features are accessible via tags.

Instead of being plain text, a tag represents
some inline content (e.g. `[audio foo]`) or
a running change (e.g. `[voice de]`).

Additionally, there is a special empty tag `[]` that
clears any running changes set by other tags.

A tag is composed of a type (case sensitive) and a possible argument (value), 
separated by a space, e.g. `[wait 0.5s]` is a tag of type `wait` with a value of `0.5s`.
A tag with no explicit argument, like `[foo]`, has an empty string as its value.

Invalid syntax (nested tags, unclosed tags, unknown tags) is skipped over.


## Built-in tags

A few tags are built:
- `[ipa {text}]` - Explicit phonetic spelling
- `[voice {hint}]` - A voice change, running change.
- `[audio {hint}]` or `[sound {hint}]` - A sound effect
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


## Custom Tags

Custom tags serve as macros/shortcuts.
In a way, they inline additional text content, according to the custom rules.

Technically, shortcuts are achievable with text replacements alone,
however additional rules apply to tags:

1. The rules are local to the tag, meaning:
   - the rules only active inside the tag of the specified type and
   - the rules don't match anything outside of that tag.
2. Each rule is only applied once per tag.
   - The rules are still applied in the order they are in the table.
3. New tags are not recognized until all the rules have been checked, meaning
   - the `[` and `]` characters are treated as text during matching and
   - the markup syntax only become final after the last rule.
4. After the rules are applied, the processed content becomes plain text.
   - Event if no rule applied to a piece of text within the argument, it is still retained.
5. Text replacements still apply after the rules, but locality is still in effect:
   - Text replacements do not match across tag boundaries,
     even after the tag is replaced with its processed argument.

Rules with empty patterns are skipped.

No rules are applied to built-in tags.


### Tricks

- Multiple rules can exist for the same tag type and will be applied in order.
- If the argument is irrelevant, this pattern - `^.*$` - always produces 1 match, no matter the input.
- If one rule matches the output of another unintentionally, there is a botch to fix that:
  - This pattern - `(?<!{[^}]*)THING(?!}[^{]*)` - matches `THING` that is not
    inside of curly braces `{}`.
  - This trick can be used to effectively isolate the output of a rule by
    putting it inside braces, which can be removed at the very end with a separate rule.

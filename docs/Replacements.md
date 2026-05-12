# Text Replacements

Portions of written text can be automatically replaced.
This allows for pronunciation corrections.

The matching pattern is written as a regex.
Each pattern is applied to text that is between tags and message bounds.
A pattern never matches tags.

The replacement process runs multiple times (multiple passes),
meaning the replacement text may match some other pattern and
be replaced again. The replacement text may also contain tags.

Patterns are applied in order they are placed in the table.

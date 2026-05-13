# Text Replacements

Portions of written text can be automatically replaced.
This allows for pronunciation corrections.

Each pattern is applied to text that is between tags and message bounds.
A pattern never matches tags, however the replacement text may contain tags.

The replacement process runs multiple times (multiple passes), meaning
the replacement text may match some other pattern and be replaced again. 

Patterns are applied in the order they are placed in the table.


### RegEx

The pattern and replacement are written as regular expressions (regex).
This allows for both simple and complex substitutions.

Replacing capture groups is supported.

You can use the following tool to create and understand regex replacements:
https://regex101.com/substitution?flags=gsi&flavor=dotnet


### Examples

If this is the replacement table:

| Pattern              | Replacement |
| -------------------- | ----------- |
| `bigram`             | `byegram`   |
| `wo*w`               | `wow`       |
| `(i like) (oranges)` | `$1 apples` |

then the following changes occur:

| What is typed in                       | What is spoken                       |
| -------------------------------------- | ------------------------------------ |
| `A lot of bigram tiles, huh.`          | `A lot of byegram tiles, huh.`       |
| `Wooooooowie`                          | `wowie`                              |
| `I like Oranges. Yes, I like oranges.` | `I like apples. Yes, I like apples.` |


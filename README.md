# TypeToSqaud
 An app to forward text to speech into virtual microphones. Inspired by Type to Voice Chat.



## What is this

This app will speak out what you type in. You can use it in combination with a virtual microphone to speak to your friends over voice chat by typing, if you need to be quiet IRL for one reason or another.

This app does not come with it's own virtual microphone.
If you want to use this app as a "microphone", you need a virtual audio input, like **VB-Audio Cable** or the virtual output of **Voicemeeter**.

The app uses TTS voices installed on your system.



## Advanced Features

Though more technical, in addition to writing simple text, you can also:

- have automatic text replacements (macros and pronunciation corrections),
- use IPA directly via the `[ipa]` tag,
- change the voice mid-message via the `[voice]` tag,
- add pauses and sound effects via `[wait]` and `[sound]` tags.

Some of the above requires setting up, like adding shorthands for sound effect paths.
See [/docs/Replacements](./docs/Replacements.md) and [/docs/MarkupTags](./docs/MarkupTags.md) for further information.

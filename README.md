# ClipCMD

> A simple utility program to simplify your workflow

## What is it?

ClipCMD is a window app that allows you to create macros and run them by copying them to the clipboard.
You write a macro anywhere, copy it to your clipboard and it will be automatically replaced by the specified macro text.

### Example

> Prefix: `_`\
> Suffix: `_`\
> Macro name: `email`\
> Macro text: `myCoolEmail@mail.com`\

> Input: `_email_`
> Output: `myCoolEmail@mail.com`

## Usage

1. Download one of the [releases](https://github.com/Stone-Red-Code/ClipCMD/releases)
1. Start `ClipCMD.exe`
1. (Optional) Set the prefix and suffix for your commands
1. Specify some commands in the built in text field
   - Format: `[macroName]>[macroText]`
   - Each line is a new macro
1. There are some built in macros like `time` and `date` which will output the current local time and date
1. Write `[suffix][macroName][preffix]` somewhere, and then select it and press ``CTRL+C``

## Preview

<!--insert image-->
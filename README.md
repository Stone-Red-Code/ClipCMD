# ClipCMD

> A simple utility program to simplify your workflow

## What is it?

ClipCMD is a Windows app that allows you to create macros and run them by copying them to the clipboard.\
You write a macro anywhere, copy it to your clipboard, and it will be automatically replaced by the output of the specified macro script.\
ClipCMD uses [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting) as scripting language.

### Example

> Prefix: `_`\
> Suffix: `_`\
> Macro name: `email`\
> Macro script: `Write-Output myCoolEmail@mail.com`\
> Macro script (alternative): `"myCoolEmail@mail.com"`

> Input: `_email_`\
> Output: `myCoolEmail@mail.com`

## Usage

1. Download one of the [releases](https://github.com/Stone-Red-Code/ClipCMD/releases) by using the installer or download it with [HyperbolicDownloader](https://github.com/Stone-Red-Code/HyperbolicDownloader)
1. Start `setup.exe`
1. (Optional) Set the prefix and suffix for your commands
1. Specify some commands in the built-in text field
   - Format: 
      ```
      [<command name(s) (separated by comma)>]
      <scriptText>
      <scriptText>
      <scriptText>
      ...
      [<command name(s) (separated by comma)>]
      <scriptText>
      <scriptText>
      <scriptText>
      ...
      ```
   - Strings between square brackets define macros.
   - You can define macro aliases by separating them by a comma `[quit, exit, q]`
   - Lines between a macro definition and the next macro (or the end of the file) is PowerShell code.
   - Everything you write to the output stream replaces the input macro.
   - You can pass parameters to the scripts by separating the parameters by space after the macro: `_combine hi nope_`
   - You can access the parameters by accessing the `$args` array.
1. There is one prebuilt macro named `list`. This lists all of your macros.
1. Write `[suffix][macroName][preffix]` somewhere, and then select it and press `CTRL+C`

## Preview

<img width="591" alt="image" src="https://user-images.githubusercontent.com/56473591/226146460-d9d2e8fc-3754-44c7-bc43-2e32f1d07847.png">

## Third party licences
- [InputSimulator](https://github.com/michaelnoonan/inputsimulator) - [MIT](https://github.com/michaelnoonan/inputsimulator/blob/master/LICENSE)


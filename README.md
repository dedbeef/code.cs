# code.cs (plugin for `ok`)
A quick C# (compile and execute on the fly) plugin for `ok`:
https://github.com/dedbeef/ok

#### Requirements
- .NET 4.8
- Windows 7/10/11
- Some way to clone the repo, e.g. `git` or `Visual Studio`.
- Already have the `ok` repo cloned (https://github.com/dedbeef/ok)

#### Terms
**default repo folder** - Usually `C:\Users\Username\source\repos\`

## Installation
1. Clone (or fork then clone) this repo using Visual Studio to the default repo folder.
   - `https://github.com/dedbeef/code.cs.git`
2. Build it if it does not already have binaries included.
   - `ok` will automatically find these cloned repos and the binaries to use

## Usage
1. Open a command prompt or powershell window anywhere.
2. Run the command with `ok`
   - e.g. `ok code.cs edit MyScript`
   - e.g. `ok code.cs run MyScript`
   - e.g. `ok code.cs list`
   - Type `ok code.cs` to see the syntax for more info.
   

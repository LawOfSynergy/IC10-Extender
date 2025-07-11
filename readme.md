# Setting up this workspace

This mod involves transpiling the ProgrammableChip class, most of whose internals are private. So in order to 
be able to access the internal classes, we have to first generate a publicized version of the dll. This dll 
will be used for linking, but it is NOT required to replace the game's version of the dll with this publicized 
version.

## Installing the Publicizer

We will be using [BepInEx.AssemblyPublicizer](https://github.com/BepInEx/BepInEx.AssemblyPublicizer/tree/master) 
to generate our publicized dependency. I prefered to use the cli version.

Install the tool with `dotnet tool install -g BepInEx.AssemblyPublicizer.Cli`

## Generating Publicized Dependency

In a terminal, navigate to the dependency we need to publicize. On Windows, this will be at:
`C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\Managed`

Run `assembly-publicizer .\Assembly-CSharp.dll`

This will create the file `Assembly-CSharp-publicized.dll`. Move this file into the publicized-dlls directory of 
this project.
# For Mod Developers

## Setup

### Non-[Unity](https://github.com/ilodev/StationeersMods/blob/main/doc/CREATE-MOD-UNITY.md) Mod

If you are creating a mod that does not add any prefabs or otherwise require unity, then your project setup should be pretty simple.

Open Visual Studio (2022), and create a new Class Library and ignore the Framework for now. (We will modify that in the next step)

Open your .csproj file and should have a section that looks something like this:

```xml
<PropertyGroup>
	<TargetFramework>net8.0</TargetFramework>
	<RootNamespace>IC10E_Direct_Reference_Extension</RootNamespace>
	<GameDir>C:\Program Files (x86)\Steam\steamapps\common\Stationeers</GameDir>
	<DebugType>embedded</DebugType>
</PropertyGroup>
```

Change TargetFramework to `net4.8`

Later in the file, there may be an `<ItemGroup>` section, where your dependencies are listed. If one doesn't exist,
add the following:

```
<ItemGroup>
    <PackageReference Include="ic10extender" Version="1.0.1" />
</ItemGroup>
```

Otherwise, just insert the `<PackageReference>` line above. That will add this framework as a dependency from nuget.

### [Unity](https://github.com/ilodev/StationeersMods/blob/main/doc/CREATE-MOD-UNITY.md) Mod

If you are creating a mod that does add prefabs, or otherwise requires unity, then your project setup will be a little less
straightforward, since the .csproj file gets regularly regenerated.

In this case, you will need to get the IC10Extender.dll file. You can do so by 
[subscribing](https://steamcommunity.com/sharedfiles/filedetails/?id=3528329094) to the mod on the workshop, navigating to its folder,
and copying the dll.

You will need to paste this to your `Assets/Assemblies/` folder.

### Creating an OpCode

Adding an opcode has three parts.
- The `Operation` abstract class represents the actual unit of work that is carried out when the opcode gets executed by the programmable chip.
- The `ExtendedOpCode` abstract class is a factory class. It is used to retrieve information about the operation, and to acquire instances of the operation it represents.
- Registering the opcode using `IC10Extender.Register`. This takes an instance of your sublcass of ExtendedOpCode.

Here is a simple example:

```C#
namespace MyNamespace
{
    public class MyOpCode : ExtendedOpCode
    {
        private static readonly HelpString[] Args = { /*An array of HelpStrings that describe what each argument is*/

        public ThrowOperation() : base(/*the actual opcode string*/) { }

        public override void Accept(int lineNumber, string[] source)
        {
            //This is your opportunity to throw a ProgrammableChipException to reject a line as not being correct
            //source includes the opcode itself, not just the args, so you need to take it into account when counting arguments
            if (source.Length != Args.Length + 1) throw new ProgrammableChipException(ICExceptionType.IncorrectArgumentCount, lineNumber);
        }

        // Returns an instance of the operation, using the text that was previously fed into the Accept() method
        public override Operation Create(ChipWrapper chip, int lineNumber, string[] source)
        {
            return new Instance(chip, lineNumber, source);
        }

        // This is used to create the help string that shows you what args are left while you are typing in the editor.
        // currentArgCount is mainly there to support varargs if your command needs it and does NOT include the opcode itself in the count.
        // Otherwise, it can be ignored.
        public override HelpString[] Params(int currentArgCount)
        {
            return Args;
        }

        // If you would like to change what color the opcode is highlighted as, you can also override the Color() method

        public class Instance : Operation
        {
            public Instance(ChipWrapper chip, int lineNumber, /*string arg1, string arg2, etc. or just string[] args*/*) : base(chip, lineNumber)
            {
                //set up your variables
            }

            public override int Execute(int index)
            {
                //your actual execution code
            }
        }
    }
}
```

You can of course, organize this however you want, but this is a nice, relatively self contained way of doing it.

Finally, in your mod entrypoint, just add `IC10Extender.Register(new MyOpCode());`

### Using this as a soft dependency

[This](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Mod-Compatibility%3A-Soft-Dependency/) is a decent article on how to add a soft dependency

Additionally, [IC10Inspector](https://github.com/RozeDoyanawa/StationeersIC10Inspector/blob/1f41dc6dfaaa23ff0f9c5236e26096bb4a7a6fe9/Assets/Scripts/IC10Inspector.cs#L44) is a working
example of this library being used as a soft dependency.

### Understanding the various Operation.XXXXVariable classes

TBWritten as I am still working on fully understanding them myself.
However, you can look at [Direct Reference Extensions](https://github.com/LawOfSynergy/IC10E_Direct_Reference_Extension/blob/main/Plugin.cs) or crack
open the game's Assembly-CSharp.dll file with a tool like dotPeek and poke around in Assets.Scripts.Objects.Electrical.ProgrammableChip for examples
of them in use.


# For Contibutors (or building locally)

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

# Acknowledgements
Special thanks to tom_is_unlucky over on the stationeers modding discord for helping to debug the transpiler!

And of course, thanks to the other lovely people on that discord who have been helpful in getting this where it is.
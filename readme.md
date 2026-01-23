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

## Creating an OpCode

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

### Opcode Lifecycle operations

This library now supports the ability to inject logic before or after the execution of an opcode. This can be done globally by adding a delegate to `IC10Extender.PreExecute` or 
`IC10Extender.PostExecute`, per chip using the same fields on the `ChipWrapper` instance, or per line using the same fields on the `Line` instance.

e.g.

```c#
public void LogExec(OpContext op, ref int index) {
    Logger.LogInfo($"Executed: {op.Raw}, jumped to {index} afterward");
}

// somewhere else in your code
IC10Extender.PostExecute+= LogExec;
```

#### Type Checking operations

In order to allow for lifecycle operations on vanilla commands (technically, any operation not registered with this library, not just vanilla), all operations will be wrapped in an OpContext
constructed from the original operation and the Line associated with this LineOfCode. Strictly speaking, the OpContext itself is also wrapped by an OperationWrapper in order to be assignable to
the `ProgrammableChip._Operation` class. So in general the object wrapping chain will look like one of the following:

```
# if the leaf operation is from this library
ProgrammableChip.LineOfCode -> OperationWrapper -> OpContext -> <author defined Operation subclass>

# otherwise (vanilla, or modded and directly subclassing ProgrammableChip._Operation)
ProgrammableChip.LineOfCode -> OperationWrapper -> OpContext -> ReverseWrapper -> <vanilla or externally modded _Operation subclass>
```

## Creating a Preprocessor

A preprocessor is used to do manipulation of the source lines before it gets translated into Operation instances. Several preprocessors exist by default. They are executed in the order
that they appear in the preprocessor list. An optional index has been provided so that mod authors can insert their preprocessor before another if necessary. The default processors, in order
of registration/execution are:

- CommentPreprocessor
- StringPreprocessor (beta branch only at time of writing)
- HashPreprocessor
- BinaryLiteralPreprocessor
- HexLiteralPreprocessor
- LabelPreprocessor

Preprocessors are structured similarly to ExtendedOpCodes in that the Preprocessor class is your factory and will provide information around what its PreprocessorOperation instances do.
Unlike ExtendedOpCode, however, they operate on all of the lines simultaneously, presented to them as a list of Line structs. The simplest implementation of a preprocessesor operation only
needs to implement the `Line? ProcessLine(Line line);` method. This processes an individual line. By default if null is returned, then the line is deleted from the list, and will not be carried
forward for further processing. More complicated preprocessor implementations can override `IEnumerable<Line> DoPass(IEnumerable<Line> fullScript)` or 
`IEnumerable<Line> Process(IEnumerable<Line> fullScript)` for more control as needed.

## Custom Constants

This library has a mechanism for registing custom constants, however I have not explicitly patched them into usage yet, so I am not sure this system is currently functional.

## Compatability Constraints

If you have opcodes or preprocessors that depend on the presence or absence of specific functionality, you can optionally specify a lambda function during registration that checks for the requirement.
You can use instances of the Compatability class for already defined checks, or define your own named checks.

## Using this as a soft dependency

[This](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Mod-Compatibility%3A-Soft-Dependency/) is a decent article on how to add a soft dependency

Additionally, [IC10Inspector](https://github.com/RozeDoyanawa/StationeersIC10Inspector/blob/1f41dc6dfaaa23ff0f9c5236e26096bb4a7a6fe9/Assets/Scripts/IC10Inspector.cs#L44) is a working
example of this library being used as a soft dependency.

### Understanding the various Operation.XXXXVariable classes

TBWritten as I am still working on fully understanding them myself.
However, you can look at [Direct Reference Extensions](https://github.com/LawOfSynergy/IC10E_Direct_Reference_Extension/blob/main/Plugin.cs) or crack
open the game's Assembly-CSharp.dll file with a tool like dotPeek and poke around in Assets.Scripts.Objects.Electrical.ProgrammableChip for examples
of them in use.

A future update is planned to include a fluent style builder to make the construction and usage of these variables more intuitive/understandable.


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
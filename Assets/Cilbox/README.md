# Cilbox

## This is WILDLY incomplete, and there are some major parts that will change.

### Cilbox has
 * `CilboxableAttribute` So you can add `[Cilboxable]` to your class as an attribute that will tell Cilbox to emulate it.
 * `CilboxProxy` - The MonoBehaviour that replaces your script.
 * `CilboxClass` - for holding information about classes that are being overridden.
 * `Cilbox` - A static thing for managing the whole system.

### The general approach
1. You mark your class as `[Cilboxable]`
2. On build, any components with a `[Cilboxable]` script are serialized, and replaced with a `CilboxProxy` which has a public String containing the serialized data for all of its properties.
3. All classes that are `[Cilboxable]` are reflected and all fields information and method bytecode is extracted and saved off as a serialized ball of goo.
4. All `[Cilbox`ed classes are loaded out of the ball of goo into a dictionary of classes where the bytecode is kept as byte arrays, and static members are configured with their appropriate types.
5. When running, each gameobject with a `CilboxProxy` will wake up, and load into a series of `object`s the data that was part of the original class.
6. Whenever `Start` or `Update` is called, the `CilboxProxy` will ask `Cilbox` to emulate the bytecode associated with that method.
7. The bytecode emulator can make all needed decisions about proper sandboxing, infinte loop termination, etc. But for the most part, my intent is to just disable any code that's doing something that could be usnafeish.

## Cleanup
 * Make function for constructors and methods, so we can get both at once.
 * Improve the definitions for serializing the list of data for Methods, Fields, and Strings.  Maybe encapsulate again.
 * Make it so we can initialize before Start.  Waiting til Start is very depressing.
 * Figure out where "The referenced script (Unknown) on this Behaviour is missing!" is coming from.

## TODO
 * Use Harmony to prevent execution of original script .ctor and Awake() i.e. near `CilboxScenePostprocessor` https://github.com/MerlinVR/UdonSharp/blob/master/Packages/com.merlin.UdonSharp/Editor/UdonSharpEditorManager.cs#L145
 * Support ref.
 * Make mechanism to extract strings as metadata's
 * Make all metadata's patchable.
 * Fixup arithmatic functions to do the right thing.
 * Do the rest of the opcodes.

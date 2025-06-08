# Cilbox

**This is incomplete, and portions will change.**

## Design

There is a `Cilbox` object that gets placed in the scene at runtime. This box
is responsible for loading and manging the various `CilboxProxy` classes that
are converted.  There can be multiple cilboxes one per scene loaded in a world.
Ideally the world could have a `Cilbox` and an avatar could also have a `Cilbox`

### The general approach
1. You mark your class as `[Cilboxable]`
2. On build, any components with a `[Cilboxable]` script are serialized, and replaced with a `CilboxProxy` which has a public String containing the serialized data for all of its properties.
3. All classes that are `[Cilboxable]` are reflected and all fields information and method bytecode is extracted and saved off as a serialized ball of goo.
4. All `[Cilbox`ed classes are loaded out of the ball of goo into a dictionary of classes where the bytecode is kept as byte arrays, and static members are configured with their appropriate types.
5. When running, each gameobject with a `CilboxProxy` will wake up, and load into a series of `object`s the data that was part of the original class.
6. Whenever `Start` or `Update` is called, the `CilboxProxy` will ask `Cilbox` to emulate the bytecode associated with that method.
7. The bytecode emulator can make all needed decisions about proper sandboxing, infinte loop termination, etc. But for the most part, my intent is to just disable any code that's doing something that could be usnafeish.

### Cilbox has
 * `CilboxableAttribute` So you can add `[Cilboxable]` to your class as an attribute that will tell Cilbox to emulate it.
 * `CilboxProxy` - The MonoBehaviour that replaces your script.
 * `Cilbox` - A static thing for managing the whole system.

### Cilbox internally uses
 * `CilboxMethod` - for holding information about classes that are being overridden.
 * `CilboxClass` - for holding information about classes that are being overridden.
 * `StackElement` - a generic "object" like thing that can be written into/altered/etc, without needing to box/unbox/etc.
 * These Stack Elements can be: `Boolean`, `Sbyte`, `Byte`, `Short`, `Ushort`, `Int`, `Uint`, `Long`, `Ulong`, `Float`, `Double`, `Object`, `Address`
 * If the StackElement is an `Obejct`, boxing will need to happen when it gets used.
 * If the StackElement is an `Array`, then it is actually a reference, where .o contains the link to the `Array` and .i contains the reference to the element.

### Security
 * You must implement the following three functions:
   * `GetNativeTypeFromName`
   * `TypeNamesToArrayOfNativeTypes`
   * `GetNativeMethodFromTypeAndName`

## Cleanup
 * Clean up the `GetConstructors` code to use `GetConstructor` but we need access to the modifiers.
 * Improve the definitions for serializing the list of data for Methods, Fields, and Strings in the `assemblyRoot["metadata"]`. Maybe encapsulate again.  
 * Make it so we can initialize before Start.  Waiting til Start is very depressing.
 * Try to make sense of when we should be init/start/awakening'ing.
 * Figure out where "The referenced script (Unknown) on this Behaviour is missing!" is coming from.

## TODO
 * Validate that you are working with int's more
 * Consider force-cleaning-up .o's when loading StackElements. << Ok, I really think we should do that.
 * Write filter system to allow for security to host functions.
 * Use a different serialization mechanism. (Preferably size/text).  The current one DOMINATES build / startup time.
 * Find all references in the scene to the original scripts, and port them over to the proxy scripts.
 * Test support for ref.
 * Optimize
   * trick the ldelem.
   * functions to fast-path if the array type is StackElement[]
 * Do the rest of the opcodes.
 * Write a good version of `DeserializeDataForProxyField` that can handle various data types, like Vector3, etc...
 * Make it so you can access non-proxy object fields in other gameobjects (selectively)
 * Need types to also be searched out and destroyed, for things like unbox, etc.  So we need a new section in metadata for types.
 * WRITE LOTS OF TESTS


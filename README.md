# Cilbox

**This is incomplete, and portions will change.**

`Cilbox` is a CIL emulator, geared for Unity. It allows the execution of arbitrary CIL code in a relatively sandboxed manner. In that whatever access is granted to types/methods, the script running within the sandbox can access.  This is NOT ia JITter.  This does not use fancy language constructs, and works (actually works best) in IL2CPP.

The performance is surprisingly high for something that is written in C# and not JITting.

Performance has been tested on Synergiance's 6502 emulator.  It is able to just barely hit `2MHz` in-system. While running the 6502, verses about `240MHz` native, the rate that this system executes CIL insturctions is about `168 MHz`, when targeting IL2CPP in Unity 6.1.2f1 running on a 9950X3D.

## Security Disclaimer

I am not a security researcher. I cannot make promises about the security of the core portion of the engine. I also cannot make claims to the fitness or suitability for any purpose.  There are likely bugs, so if you have one, I strongly encourage submitting a pull request.

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

### Things you can't do (At least not today)
 * You cannot have arrays of properties on your object, for instance an array of GameObjects.  Each property must be a regular property.
 * You cannot arbitrarily add an externally accessable method to your script. For instance, you cannot add your script to Unity UI and select a function that is not available in the `CilboxProxy`
 * It is unlikely any form of reflection would be possible, because, it would be extremely difficult to secure.
 * It will be tricky to allow compound types for security reasons.
 * You can't currently reference fields of objects outside Cilbox, but you can access properties that have getters/setters.

## Cleanup
 * Clean up the `GetConstructors` code to use `GetConstructor` but we need access to the modifiers.
 * Improve the definitions for serializing the list of data for Methods, Fields, and Strings in the `assemblyRoot["metadata"]`. Maybe encapsulate again.  
 * Make it so we can initialize before Start.  Waiting til Start is very depressing.
 * Try to make sense of when we should be init/start/awakening'ing.
 * Figure out where "The referenced script (Unknown) on this Behaviour is missing!" is coming from.



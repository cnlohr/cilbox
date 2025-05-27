# Cilbox

Cilbox has:
 * `CilboxableAttribute` So you can add `[Cilboxable]` to your class as an attribute that will tell Cilbox to emulate it.
 * `CilboxProxy` - The MonoBehaviour that replaces your script.
 * `CilboxClass` - for holding information about classes that are being overridden.
 * `Cilbox` - A static thing for managing the whole system.

## TODO
 * Use Harmony to prevent execution of original script .ctor and Awake() i.e. near `CilboxScenePostprocessor` https://github.com/MerlinVR/UdonSharp/blob/master/Packages/com.merlin.UdonSharp/Editor/UdonSharpEditorManager.cs#L145

 * Support ref.
 * Make mechanism to extract strings as metadata's
 * Make all metadata's patchable.

 * The Cilbox should probably be able to exist one per user, instead of static.

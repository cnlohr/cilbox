using System;
using System.Reflection;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class HideInInspectorAttribute : Attribute { }

namespace Unity
{
	namespace Profiling
	{
		public class ProfilerMarker
		{
			public ProfilerMarker( String s ) { }
			public void Begin() { }
			public void Auto() { }
			public void End() { }
		}
	}
}

namespace UnityEditor
{
	namespace Build
	{
		public class IProcessSceneWithReport
		{
		}
		public class IPreprocessBuildWithReport
		{
		}
		public class Reporting
		{
			public class BuildReport
			{
			}
		}
	}

	namespace Callbacks
	{
		public class Callbacks
		{
		}
	}

	namespace SceneManagement
	{

		public class EditorSceneManager
		{
			public static void MarkSceneDirty( UnityEngine.SceneManagement.Scene s ) { }
			public static void SaveScene( UnityEngine.SceneManagement.Scene s ) { }
		}
	}

}

namespace UnityEngine
{
	public class MonoScript
	{
		public static MonoScript FromMonoBehaviour( MonoBehaviour mb ) { return null; }
	}

	public class AssetDatabase
	{
		public static String GetAssetPath( MonoScript m ) { return "."; }
	}

	public class Events
	{
		public delegate void UnityAction();
		//public static explicit operator System.Action(UnityAction obj) { return new System.Action(()=>{obj();}); }
	}

	// Shim for the moment (Yes, it abstracts from component, but for now...)
	public class Collider
	{
		// Nothing.
	}

	public static class Application
	{
		public static String dataPath = ".";
		public static event Events.UnityAction onBeforeRender;
		public static void CallBeforeRender() { if(onBeforeRender != null) onBeforeRender(); }
	}

	public static  class Resources
	{
		public static MonoBehaviour [] FindObjectsOfTypeAll( Type t )
		{
			int count = 0;
			foreach( GameObject o in GameObject.AllObjects )
			{
				foreach( MonoBehaviour m in o.AllComponents )
				{
					if( t.IsAssignableFrom(m.GetType()))
						count++;
				}
			}

			Array ret = Array.CreateInstance(t, count);
			count = 0;
			foreach( GameObject o in GameObject.AllObjects )
			{
				foreach( MonoBehaviour m in o.AllComponents )
				{
					if( t.IsAssignableFrom(m.GetType()))
						ret.SetValue( m, count++ );
				}
			}
			return (MonoBehaviour[])ret;
		}
	}

	class EditorUtility
	{
		public static void SetDirty( MonoBehaviour m ) { }
	}

	namespace SceneManagement
	{

		public class SceneManager
		{
			public static Scene GetActiveScene() { return null; }
		}
		public class Scene
		{
			public GameObject[] GetRootGameObjects() { return new GameObject[0]; }
		}
	}

	public class SerializeField
	{
	};


	public class MonoBehaviour : Object
	{
		public GameObject gameObject;
		public MonoBehaviour() { }
	}

	public class Debug
	{
		public static void LogError( String s ) { Console.WriteLine( s ); }
		public static void LogWarning( String s ) { Console.WriteLine( s ); }
		public static void Log( String s ) { Console.WriteLine( s ); }
	}

	public enum HideFlags
	{
		HideInHierarchy
	}

	public class Vector3
	{
		public float x, y, z;
		public Vector3( float x, float y, float z ) { this.x = x; this.y = y; this.z = z; }
	}

	public static class Random
	{
		static public int Range( int mi, int ma )
		{
			return 5; // Determined by radom dice roll
		}
	}

	public class Transform
	{
		public Vector3 position;
		public GameObject parent = null;
	}

	public class Component
	{
	}

	public class GameObject : UnityEngine.Object
	{
		public static List<GameObject> AllObjects = new List<GameObject>();
		public List<MonoBehaviour> AllComponents = new List<MonoBehaviour>();
		public String name;
		public HideFlags hideFlags;
		public Transform transform = new Transform();

		public MonoBehaviour[] GetComponentsInChildren<T>( bool something ) { return new MonoBehaviour[0]; }

		public GameObject gameObject = null;

		public static void Destroy( GameObject o ) { AllObjects.Remove( o ); }
		public GameObject(String name) { this.name = name; GameObject.AllObjects.Add(this);  }

		public static GameObject Find( String s )
 		{
			foreach( GameObject o in AllObjects )
			{
				if( o.name == s ) return o;
			}
			return null;
		}

		public bool TryGetComponent<T>( out T ret ) { ret = default(T); return false; }
		public bool TryGetComponent( Type typ, out Component ret ) { ret = null; return false; }
		public T GetComponent<T>() { return default(T); }
		public Component GetComponent(Type typ) { return null; }

		public static implicit operator bool(GameObject obj) { return obj != null; }

		public T CreateComponent<T>() where T: MonoBehaviour, new() {
			T ret = (T)AddComponent( typeof(T) );
			return (T)ret;
		}

		public T AddComponent<T>() where T : MonoBehaviour
		{
	        ConstructorInfo ctor = typeof(T).GetConstructor(new Type[]{});
			T m = (T)ctor.Invoke(new object[] {});
			m.gameObject = this;
			AllComponents.Add(m);
			return (T)m;
		}

		public MonoBehaviour AddComponent( Type t )
		{
	        ConstructorInfo ctor = t.GetConstructor(new Type[]{});
			MonoBehaviour m = (MonoBehaviour)ctor.Invoke(new object[] {});
			m.gameObject = this;
			AllComponents.Add(m);
			return m;
		}

		public static T [] FindObjectsByType<T>(FindObjectsSortMode sm) where T:GameObject
		{
			List<T> ret = new List<T>();
			foreach( var o in AllObjects )
			{
				if( typeof(T).IsAssignableFrom(o.GetType() ))
				{
					ret.Add( (T)o );
				}
			}
			return ret.ToArray();
		}
		public T [] GetComponents<T>() where T:MonoBehaviour
		{
			List<T> ret = new List<T>();
			foreach( var o in AllComponents )
			{
				if( typeof(T).IsAssignableFrom(o.GetType() ))
				{
					ret.Add( (T)o );
				}
			}
			return ret.ToArray();
		}
	}

	public enum FindObjectsSortMode
	{
		None
	};

	public class Object
	{
		public static implicit operator bool(Object obj) { return obj != null; }
		public static void DestroyImmediate( MonoBehaviour m ) { GameObject.Destroy(m.gameObject); }
	}

	static public class JsonUtility
	{
		public static string ToJson( object o ) { return ""; }
		public static object FromJson( string s, Type t ) { return null; }
	}

	// We don't _really_ do serialization.
}

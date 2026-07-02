#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Cilbox
{
	public static class CilboxCompatAnalyzer
	{
		const string kAutoRunPref = "Cilbox.CompatAnalyzer.RunOnReload";
		const string kMenuRun     = "Tools/Cilbox/Check Sandbox Compatibility";
		const string kMenuAuto    = "Tools/Cilbox/Check Compatibility On Each Reload";
		const int    kMaxReported = 300;

		[DidReloadScripts]
		static void OnScriptsReloaded()
		{
			if( !EditorPrefs.GetBool( kAutoRunPref, true ) ) return;
			EditorApplication.delayCall += () =>
			{
				if( EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode ) return;
				try { Analyze( false ); }
				catch( Exception e ) { Debug.LogError( "[Cilbox compat] analyzer crashed: " + e ); }
			};
		}

		[MenuItem( kMenuRun, false, 2000 )]
		static void MenuRun() { Analyze( true ); }

		[MenuItem( kMenuAuto, false, 2001 )]
		static void MenuToggleAuto()
		{
			EditorPrefs.SetBool( kAutoRunPref, !EditorPrefs.GetBool( kAutoRunPref, true ) );
		}
		[MenuItem( kMenuAuto, true )]
		static bool MenuToggleAutoValidate()
		{
			Menu.SetChecked( kMenuAuto, EditorPrefs.GetBool( kAutoRunPref, true ) );
			return true;
		}

		class OpcodeIssue
		{
			public string typeFullName;
			public string typeDisplay;
			public string method;
			public int    firstIlOffset;
			public string opDisplay;
			public string opName;
			public int    count;
		}

		class GapIssue
		{
			public string typeFullName;
			public string typeDisplay;
			public string method;
			public int    ilOffset;
			public string message;
			public int    count;
		}

		struct Site
		{
			public string typeFullName;
			public string typeDisplay;
			public string method;
			public int    ilOffset;
		}

		struct SceneHost
		{
			public UnityEngine.SceneManagement.Scene scene;
			public List<Type> boxTypes;
		}

		public static void Analyze( bool verbose )
		{
			List<Type> cilboxableTypes = GatherCilboxableTypes();
			if( cilboxableTypes.Count == 0 )
			{
				if( verbose ) Debug.Log( "[Cilbox compat] No [Cilboxable] types found; nothing to check." );
				return;
			}

			List<SceneHost> sceneHosts = GetLoadedSceneHosts();
			HashSet<string> sceneTypeNames = GetSceneCilboxableTypeNames( sceneHosts );

			HashSet<int> singleByte, twoByte;
			bool opcodeSetOk = TryLoadSupportedOpcodes( out singleByte, out twoByte, out string opcodeParseError );

			var opcodeIssues = new Dictionary<string, OpcodeIssue>();
			var gapIssues    = new Dictionary<string, GapIssue>();
			var memberSites  = new Dictionary<string, List<Site>>();
			int scannedMethods = 0;
			foreach( Type t in cilboxableTypes )
			{
				bool indexForAttribution = sceneTypeNames.Contains( t.FullName );
				foreach( MethodBase m in EnumerateExportedMethods( t ) )
				{
					scannedMethods++;
					WalkMethodIL( t, m, opcodeSetOk ? singleByte : null, opcodeSetOk ? twoByte : null,
						opcodeIssues, gapIssues, indexForAttribution ? memberSites : null );
				}
			}

			List<string> whitelistErrors = RunWhitelistCheck( sceneHosts );
			List<KeyValuePair<string, string>> policeIssues = RunContentPoliceCheck( sceneHosts );

			int reported = 0;
			HashSet<string> affectedScripts = new HashSet<string>();

			if( !opcodeSetOk )
				Debug.LogWarning( "[Cilbox compat] Could not derive the interpreter's supported-opcode set from Cilbox.cs (" +
					opcodeParseError + "); opcode checking was skipped this run. Whitelist checking still ran." );

			if( sceneHosts.Count == 0 && verbose )
				Debug.Log( "[Cilbox compat] No Cilbox host found in the loaded scene(s); whitelist + content-police checks were skipped. " +
					"Open the world/prop scene to run them (the opcode check above ran over all [Cilboxable] scripts)." );

			foreach( OpcodeIssue oi in opcodeIssues.Values )
			{
				if( reported++ > kMaxReported ) break;
				affectedScripts.Add( oi.typeDisplay );
				string msg =
					$"[Cilbox compat] Unimplemented IL opcode '{oi.opName}' ({oi.opDisplay}) in {oi.typeDisplay}.{oi.method}" +
					( oi.count > 1 ? $" (x{oi.count})" : "" ) +
					$" -- the Cilbox interpreter has no dispatch case for it, so this throws \"Opcode {oi.opDisplay} unimplemented\" at runtime.";
				CilboxSourceLog.LogSourceException( msg,
					new List<CilboxSourceLog.WorldFrame> { new CilboxSourceLog.WorldFrame( oi.typeFullName, oi.method, oi.firstIlOffset ) } );
			}

			foreach( GapIssue g in gapIssues.Values )
			{
				if( reported++ > kMaxReported ) break;
				affectedScripts.Add( g.typeDisplay );
				CilboxSourceLog.LogSourceException(
					$"[Cilbox compat] {g.message}, in {g.typeDisplay}.{g.method}" + ( g.count > 1 ? $" (x{g.count})" : "" ) + " -- throws at runtime.",
					new List<CilboxSourceLog.WorldFrame> { new CilboxSourceLog.WorldFrame( g.typeFullName, g.method, g.ilOffset ) } );
			}

			List<Type> sceneTypes = cilboxableTypes.FindAll( x => sceneTypeNames.Contains( x.FullName ) );
			List<string> framelessWl;
			var whitelistDiags = BuildWhitelistDiagnostics( whitelistErrors, memberSites, sceneTypes, affectedScripts, out framelessWl );
			foreach( KeyValuePair<string, List<string>> wl in whitelistDiags )
			{
				if( reported++ > kMaxReported ) break;
				CilboxSourceLog.LogSourceExceptionLines( "[Cilbox compat] Cilbox whitelist violation: " + wl.Key, wl.Value, false );
			}
			foreach( string fw in framelessWl )
			{
				if( reported++ > kMaxReported ) break;
				CilboxSourceLog.LogSourceExceptionLines( "[Cilbox compat] Cilbox whitelist violation: " + fw, null, true );
			}

			foreach( KeyValuePair<string, string> p in policeIssues )
			{
				if( reported++ > kMaxReported ) break;
				CilboxSourceLog.LogSourceExceptionLines( "[Cilbox compat] Basis content-police: " + p.Key,
					p.Value != null ? new List<string> { p.Value } : null, true );
			}

			int whitelistCount = whitelistDiags.Count + framelessWl.Count;
			int issueCount = opcodeIssues.Count + gapIssues.Count + whitelistCount + policeIssues.Count;
			if( issueCount == 0 )
			{
				if( verbose )
					Debug.Log( $"[Cilbox compat] OK -- {cilboxableTypes.Count} [Cilboxable] type(s), {scannedMethods} method(s) are compatible with the interpreter" +
						( sceneHosts.Count > 0 ? ", the Cilbox whitelist, and the Basis content-police." : " (whitelist/content-police need a hosted scene open)." ) );
			}
			else
			{
				Debug.LogWarning( $"[Cilbox compat] {issueCount} issue(s) across {affectedScripts.Count} script(s): " +
					$"{opcodeIssues.Count} unimplemented-opcode, {gapIssues.Count} interpreter-gap, {whitelistCount} Cilbox-whitelist, {policeIssues.Count} Basis-content-police. These will fail the bake, be stripped at load, or throw at runtime." );
			}
		}

		static List<Type> GatherCilboxableTypes()
		{
			var ret = new List<Type>();
			foreach( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
			{
				Type[] types;
				try { types = a.GetTypes(); }
				catch( ReflectionTypeLoadException e ) { types = e.Types; }
				catch { continue; }
				foreach( Type t in types )
				{
					if( t == null ) continue;
					if( t.IsEnum ) continue;
					try { if( !CilboxUtil.HasCilboxableAttribute( t ) ) continue; }
					catch { continue; }
					ret.Add( t );
				}
			}
			return ret;
		}

		static IEnumerable<MethodBase> EnumerateExportedMethods( Type t )
		{
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
			MethodBase[][] groups;
			try { groups = new MethodBase[][] { t.GetMethods( flags ), t.GetConstructors( flags ) }; }
			catch { yield break; }
			foreach( MethodBase[] g in groups )
				foreach( MethodBase m in g )
				{
					if( m.DeclaringType == null || m.DeclaringType.Assembly != t.Assembly ) continue;
					yield return m;
				}
		}

		static void WalkMethodIL( Type owner, MethodBase m, HashSet<int> singleByte, HashSet<int> twoByte,
			Dictionary<string, OpcodeIssue> issues, Dictionary<string, GapIssue> gapIssues, Dictionary<string, List<Site>> memberSites )
		{
			MethodBody body;
			try { body = m.GetMethodBody(); } catch { return; }
			if( body == null ) return;
			byte[] il;
			try { il = body.GetILAsByteArray(); } catch { return; }
			if( il == null || il.Length == 0 ) return;

			Module mod = m.Module;
			Type[] typeGenArgs = SafeGenericArgs( owner );
			Type[] methGenArgs = m.IsGenericMethod ? SafeGenericArgs( m ) : null;

			int i = 0;
			while( i < il.Length )
			{
				int at = i;
				CilboxUtil.OpCodes.OpCode oc;
				try { oc = CilboxUtil.OpCodes.ReadOpCode( il, ref i ); }
				catch { break; }

				byte op1 = oc.Op1, op2 = oc.Op2;
				bool twoByteOp = op1 == 0xfe;
				if( op1 != 0xff && op1 != 0xfe )
				{
					AddOpcodeIssue( issues, owner, m.Name, at, "0x" + il[at].ToString( "X2" ), "<undecodable>" );
					break;
				}

				CilboxUtil.OpCodes.OperandType ot = oc.OperandType;
				int opLen = CilboxUtil.OpCodes.OperandLength[(int)ot];
				long operand = 0;
				try
				{
					if( ot == CilboxUtil.OpCodes.OperandType.InlineSwitch )
					{
						uint n = (uint)CilboxUtil.BytecodePullLiteral( il, ref i, 4 );
						i += (int)n * 4;
					}
					else if( opLen > 0 )
					{
						operand = (long)CilboxUtil.BytecodePullLiteral( il, ref i, opLen );
					}
				}
				catch { break; }

				if( singleByte != null )
				{
					bool supported = twoByteOp ? twoByte.Contains( op2 ) : singleByte.Contains( op2 );
					if( !supported )
						AddOpcodeIssue( issues, owner, m.Name, at,
							twoByteOp ? $"0xfe 0x{op2:X2}" : $"0x{op2:X2}", SafeName( oc ) );
				}

				if( gapIssues != null )
				{
					if( !twoByteOp && op2 == 0xD0 )
					{
						object member = TryResolveMember( mod, (int)operand, typeGenArgs, methGenArgs );
						if( member is Type mt )
							AddGap( gapIssues, owner, m.Name, at, "ldtoken:type",
								$"Unsupported ldtoken of type '{mt.FullName}' (e.g. typeof) -- Cilbox's ldtoken handler only supports field / array-initializer tokens" );
						else if( member is MethodBase )
							AddGap( gapIssues, owner, m.Name, at, "ldtoken:method",
								"Unsupported ldtoken of a method -- Cilbox's ldtoken handler only supports field / array-initializer tokens" );
					}
					else if( twoByteOp && ( op2 == 0x06 || op2 == 0x07 ) )
					{
						MethodBase mb = TryResolveMethodBase( mod, (int)operand, typeGenArgs, methGenArgs );
						if( mb != null && mb.DeclaringType != null && !SafeHasCilboxable( mb.DeclaringType ) )
							AddGap( gapIssues, owner, m.Name, at, "ldftn:" + mb.DeclaringType.FullName + "." + mb.Name,
								$"Unsupported function-pointer to native method '{mb.DeclaringType.FullName}.{mb.Name}' -- Cilbox can only reference functions inside the cilbox" );
					}
				}

				if( memberSites != null &&
					( ot == CilboxUtil.OpCodes.OperandType.InlineMethod ||
					  ot == CilboxUtil.OpCodes.OperandType.InlineField ||
					  ot == CilboxUtil.OpCodes.OperandType.InlineType ||
					  ot == CilboxUtil.OpCodes.OperandType.InlineTok ) )
				{
					IndexMemberReference( mod, ot, (int)operand, typeGenArgs, methGenArgs,
						new Site { typeFullName = owner.FullName, typeDisplay = Display( owner ), method = m.Name, ilOffset = at },
						memberSites );
				}
			}
		}

		static void AddOpcodeIssue( Dictionary<string, OpcodeIssue> issues, Type owner, string method, int il, string opDisplay, string opName )
		{
			string key = owner.FullName + "|" + method + "|" + opDisplay;
			if( issues.TryGetValue( key, out OpcodeIssue existing ) ) { existing.count++; return; }
			issues[key] = new OpcodeIssue
			{
				typeFullName  = owner.FullName,
				typeDisplay   = Display( owner ),
				method        = method,
				firstIlOffset = il,
				opDisplay     = opDisplay,
				opName        = opName,
				count         = 1,
			};
		}

		static void AddGap( Dictionary<string, GapIssue> gaps, Type owner, string method, int il, string dedupKey, string message )
		{
			string key = owner.FullName + "|" + method + "|" + dedupKey;
			if( gaps.TryGetValue( key, out GapIssue existing ) ) { existing.count++; return; }
			gaps[key] = new GapIssue { typeFullName = owner.FullName, typeDisplay = Display( owner ), method = method, ilOffset = il, message = message, count = 1 };
		}

		static object TryResolveMember( Module mod, int token, Type[] ta, Type[] ma )
		{
			try { return mod.ResolveMember( token, ta, ma ); } catch { return null; }
		}

		static MethodBase TryResolveMethodBase( Module mod, int token, Type[] ta, Type[] ma )
		{
			try { return mod.ResolveMethod( token, ta, ma ); } catch { return null; }
		}

		static bool SafeHasCilboxable( Type t )
		{
			try { return CilboxUtil.HasCilboxableAttribute( t ); } catch { return false; }
		}

		static string SafeName( CilboxUtil.OpCodes.OpCode oc )
		{
			try { return oc.Name; } catch { return "<unknown>"; }
		}

		static Type[] SafeGenericArgs( object typeOrMethod )
		{
			try
			{
				if( typeOrMethod is Type t ) return t.IsGenericType ? t.GetGenericArguments() : null;
				if( typeOrMethod is MethodBase m ) return m.GetGenericArguments();
			}
			catch { }
			return null;
		}

		static void IndexMemberReference( Module mod, CilboxUtil.OpCodes.OperandType ot, int token,
			Type[] typeGenArgs, Type[] methGenArgs, Site site, Dictionary<string, List<Site>> memberSites )
		{
			try
			{
				if( ot == CilboxUtil.OpCodes.OperandType.InlineMethod )
				{
					IndexMethod( mod.ResolveMethod( token, typeGenArgs, methGenArgs ), site, memberSites );
				}
				else if( ot == CilboxUtil.OpCodes.OperandType.InlineField )
				{
					IndexField( mod.ResolveField( token, typeGenArgs, methGenArgs ), site, memberSites );
				}
				else if( ot == CilboxUtil.OpCodes.OperandType.InlineType )
				{
					IndexType( mod.ResolveType( token, typeGenArgs, methGenArgs ), site, memberSites );
				}
				else
				{
					MemberInfo mi = mod.ResolveMember( token, typeGenArgs, methGenArgs );
					if( mi is MethodBase mb ) IndexMethod( mb, site, memberSites );
					else if( mi is FieldInfo fi ) IndexField( fi, site, memberSites );
					else if( mi is Type ty ) IndexType( ty, site, memberSites );
				}
			}
			catch { }
		}

		static void IndexMethod( MethodBase mb, Site site, Dictionary<string, List<Site>> memberSites )
		{
			if( mb == null || mb.DeclaringType == null ) return;
			AddSite( memberSites, mb.DeclaringType.ToString() + "." + mb.Name, site );
			IndexType( mb.DeclaringType, site, memberSites );
			try
			{
				foreach( ParameterInfo p in mb.GetParameters() ) IndexType( p.ParameterType, site, memberSites );
				if( mb is MethodInfo info ) IndexType( info.ReturnType, site, memberSites );
			}
			catch { }
		}

		static void IndexField( FieldInfo fi, Site site, Dictionary<string, List<Site>> memberSites )
		{
			if( fi == null || fi.DeclaringType == null ) return;
			AddSite( memberSites, fi.DeclaringType.ToString() + "." + fi.Name, site );
			IndexType( fi.DeclaringType, site, memberSites );
			IndexType( fi.FieldType, site, memberSites );
		}

		static readonly HashSet<Type> sUbiquitousTypes = new HashSet<Type>
		{
			typeof( object ), typeof( string ), typeof( void ), typeof( ValueType ), typeof( Enum ),
			typeof( Array ), typeof( Delegate ), typeof( MulticastDelegate ), typeof( decimal ),
			typeof( IntPtr ), typeof( UIntPtr ),
		};

		static void IndexType( Type t, Site site, Dictionary<string, List<Site>> memberSites )
		{
			if( t == null ) return;
			if( t.HasElementType ) t = t.GetElementType();
			if( t == null ) return;
			if( t.IsPrimitive || sUbiquitousTypes.Contains( t ) ) return;
			if( !string.IsNullOrEmpty( t.FullName ) ) AddSite( memberSites, t.FullName, site );
			AddSite( memberSites, t.ToString(), site );
		}

		static void AddSite( Dictionary<string, List<Site>> memberSites, string key, Site site )
		{
			if( string.IsNullOrEmpty( key ) ) return;
			if( !memberSites.TryGetValue( key, out List<Site> list ) ) { list = new List<Site>(); memberSites[key] = list; }
			if( list.Count >= 12 ) return;
			foreach( Site s in list )
				if( s.typeFullName == site.typeFullName && s.method == site.method ) return;
			list.Add( site );
		}

		static readonly Regex sReErrType     = new Regex( @"^TYPE FAILED CHECK: (\S+)", RegexOptions.Compiled );
		static readonly Regex sReErrNoType   = new Regex( @"Could not find type:? (\S+)", RegexOptions.Compiled );
		static readonly Regex sReErrRefType  = new Regex( @"Could not find referenced type [^/]*/([^/]+)/", RegexOptions.Compiled );
		static readonly Regex sReErrPriv     = new Regex( @"^Privilege failed for (.+)\.([A-Za-z_]\w*) (?:method|parameter|generic argument|return type)", RegexOptions.Compiled );
		static readonly Regex sReErrPrivType = new Regex( @"(?:return type|parameter \d+ type|generic argument \d+ type) (\S+)", RegexOptions.Compiled );
		static readonly Regex sReErrNoRef    = new Regex( @"Could not find reference to: \[[^\]]*\]\[([^\]]*)\]\[[^\]]*?([A-Za-z_]\w*)\s*\(", RegexOptions.Compiled );
		static readonly Regex sReErrIntMeth  = new Regex( @"Could not find internal method (.+?):[^(]*?([A-Za-z_]\w*)\s*\(", RegexOptions.Compiled );
		static readonly Regex sReErrField    = new Regex( @"(?:Illegal field reference outside of the cilbox\.|Could not find field for object type|^Field for) (.+?)\.([A-Za-z_]\w*) (?:in\b|of type)", RegexOptions.Compiled );

		class BadSet
		{
			public readonly HashSet<string> types = new HashSet<string>( StringComparer.Ordinal );
			public readonly HashSet<string> methods = new HashSet<string>( StringComparer.Ordinal );
			public readonly HashSet<string> fields = new HashSet<string>( StringComparer.Ordinal );
		}

		static BadSet ParseWhitelistErrors( List<string> raw, out List<string> frameless )
		{
			var bad = new BadSet();
			frameless = new List<string>();
			foreach( string msg in raw )
			{
				Match m;
				if( ( m = sReErrType.Match( msg ) ).Success ) { bad.types.Add( m.Groups[1].Value ); continue; }
				if( ( m = sReErrPriv.Match( msg ) ).Success )
				{
					bad.methods.Add( m.Groups[1].Value + "." + m.Groups[2].Value );
					Match mt = sReErrPrivType.Match( msg ); if( mt.Success ) bad.types.Add( mt.Groups[1].Value );
					continue;
				}
				if( ( m = sReErrNoRef.Match( msg ) ).Success )   { bad.methods.Add( m.Groups[1].Value + "." + m.Groups[2].Value ); continue; }
				if( ( m = sReErrIntMeth.Match( msg ) ).Success ) { bad.methods.Add( m.Groups[1].Value + "." + m.Groups[2].Value ); continue; }
				if( ( m = sReErrField.Match( msg ) ).Success )   { bad.fields.Add( m.Groups[1].Value + "." + m.Groups[2].Value ); continue; }
				if( ( m = sReErrRefType.Match( msg ) ).Success ) { bad.types.Add( m.Groups[1].Value ); continue; }
				if( ( m = sReErrNoType.Match( msg ) ).Success )  { bad.types.Add( m.Groups[1].Value ); continue; }
				if( msg.IndexOf( "Could not find internal type in", StringComparison.Ordinal ) >= 0 ) continue;
				frameless.Add( msg );
			}
			return bad;
		}

		static List<KeyValuePair<string, List<string>>> BuildWhitelistDiagnostics(
			List<string> raw, Dictionary<string, List<Site>> memberSites, List<Type> sceneTypes, HashSet<string> affectedScripts, out List<string> frameless )
		{
			BadSet bad = ParseWhitelistErrors( raw, out frameless );
			var result = new List<KeyValuePair<string, List<string>>>();

			foreach( string t in bad.types )
			{
				var lines = new List<string>();
				AddFieldDeclarationLines( sceneTypes, t, lines, affectedScripts );
				AddUsageLines( memberSites, t, lines, affectedScripts );
				FinishDiag( result, frameless, $"type '{t}' is not on the Cilbox whitelist", lines );
			}
			foreach( string mk in bad.methods )
				if( !bad.types.Contains( DeclaringOf( mk ) ) )
				{
					var lines = new List<string>();
					AddUsageLines( memberSites, mk, lines, affectedScripts );
					FinishDiag( result, frameless, $"method '{mk}' is not on the Cilbox whitelist", lines );
				}
			foreach( string fk in bad.fields )
				if( !bad.types.Contains( DeclaringOf( fk ) ) )
				{
					var lines = new List<string>();
					AddUsageLines( memberSites, fk, lines, affectedScripts );
					FinishDiag( result, frameless, $"field '{fk}' is not on the Cilbox whitelist", lines );
				}

			return result;
		}

		static void FinishDiag( List<KeyValuePair<string, List<string>>> result, List<string> frameless, string message, List<string> lines )
		{
			var seen = new HashSet<string>();
			var uniq = new List<string>();
			foreach( string l in lines ) if( seen.Add( l ) ) uniq.Add( l );
			if( uniq.Count > 0 )
				result.Add( new KeyValuePair<string, List<string>>( message, uniq ) );
			else
				frameless.Add( message + " (referenced indirectly)" );
		}

		static void AddUsageLines( Dictionary<string, List<Site>> memberSites, string key, List<string> lines, HashSet<string> affectedScripts )
		{
			if( !memberSites.TryGetValue( key, out List<Site> sites ) ) return;
			var frames = new List<CilboxSourceLog.WorldFrame>();
			var seen = new HashSet<string>();
			foreach( Site s in sites )
			{
				if( !seen.Add( s.typeFullName + "|" + s.method ) ) continue;
				affectedScripts.Add( s.typeDisplay );
				if( frames.Count < 20 )
					frames.Add( new CilboxSourceLog.WorldFrame( s.typeFullName, s.method, s.ilOffset ) );
			}
			lines.AddRange( CilboxSourceLog.ResolveFrameLines( frames ) );
		}

		static void AddFieldDeclarationLines( List<Type> sceneTypes, string badType, List<string> lines, HashSet<string> affectedScripts )
		{
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
			var seenFields = new HashSet<string>();
			foreach( Type owner in sceneTypes )
			{
				FieldInfo[] fis;
				try { fis = owner.GetFields( flags ); } catch { continue; }
				foreach( FieldInfo f in fis )
				{
					Type ft = f.FieldType;
					if( ft != null && ft.HasElementType ) ft = ft.GetElementType();
					if( ft == null || ft.FullName != badType ) continue;
					Type dt = f.DeclaringType ?? owner;
					if( !seenFields.Add( dt.FullName + "." + f.Name ) ) continue;
					string path = TypeScriptPath( dt );
					if( string.IsNullOrEmpty( path ) ) continue;
					affectedScripts.Add( dt.FullName );
					lines.Add( dt.FullName + "." + f.Name + " () (at " + path + ":" + FindFieldDeclarationLine( path, ft.Name, f.Name ) + ")" );
				}
			}
		}

		static readonly Dictionary<Type, string> sTypeScriptPath = new Dictionary<Type, string>();
		static string TypeScriptPath( Type t )
		{
			if( sTypeScriptPath.TryGetValue( t, out string cached ) ) return cached;
			string found = null;
			foreach( string guid in AssetDatabase.FindAssets( "t:MonoScript " + t.Name ) )
			{
				string p = AssetDatabase.GUIDToAssetPath( guid );
				MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>( p );
				if( ms != null && ms.GetClass() == t ) { found = p; break; }
			}
			sTypeScriptPath[t] = found;
			return found;
		}

		static int FindFieldDeclarationLine( string path, string typeSimpleName, string fieldName )
		{
			try
			{
				string[] lines = File.ReadAllLines( path );
				Regex nameRx = new Regex( @"\b" + Regex.Escape( fieldName ) + @"\b" );
				Regex typeRx = new Regex( @"\b" + Regex.Escape( typeSimpleName ) + @"\b" );
				for( int i = 0; i < lines.Length; i++ )
					if( nameRx.IsMatch( lines[i] ) && typeRx.IsMatch( lines[i] ) ) return i + 1;
				Regex declRx = new Regex( @"\b" + Regex.Escape( fieldName ) + @"\b\s*[;=,]" );
				for( int i = 0; i < lines.Length; i++ )
					if( declRx.IsMatch( lines[i] ) ) return i + 1;
			}
			catch { }
			return 1;
		}

		static string DeclaringOf( string memberKey )
		{
			int dot = memberKey.LastIndexOf( '.' );
			return dot >= 0 ? memberKey.Substring( 0, dot ) : memberKey;
		}

		static List<SceneHost> GetLoadedSceneHosts()
		{
			var result = new List<SceneHost>();
			int count = UnityEngine.SceneManagement.SceneManager.sceneCount;
			for( int i = 0; i < count; i++ )
			{
				UnityEngine.SceneManagement.Scene s = UnityEngine.SceneManagement.SceneManager.GetSceneAt( i );
				if( !s.IsValid() || !s.isLoaded ) continue;
				var boxTypes = new List<Type>();
				foreach( GameObject root in s.GetRootGameObjects() )
				{
					foreach( Cilbox box in root.GetComponentsInChildren<Cilbox>( true ) )
					{
						if( box == null ) continue;
						Type t = box.GetType();
						if( !t.IsAbstract && !boxTypes.Contains( t ) ) boxTypes.Add( t );
					}
				}
				if( boxTypes.Count > 0 )
					result.Add( new SceneHost { scene = s, boxTypes = boxTypes } );
			}
			return result;
		}

		static HashSet<string> GetSceneCilboxableTypeNames( List<SceneHost> sceneHosts )
		{
			var names = new HashSet<string>( StringComparer.Ordinal );
			foreach( SceneHost sh in sceneHosts )
				foreach( GameObject root in sh.scene.GetRootGameObjects() )
					foreach( Component comp in root.GetComponentsInChildren<Component>( true ) )
					{
						if( comp == null ) continue;
						Type ct = comp.GetType();
						if( CilboxUtil.HasCilboxableAttribute( ct ) && !string.IsNullOrEmpty( ct.FullName ) )
							names.Add( ct.FullName );
					}
			return names;
		}

		static List<string> RunWhitelistCheck( List<SceneHost> sceneHosts )
		{
			var errors = new List<string>();

			foreach( SceneHost sh in sceneHosts )
			{
				CilboxScenePostprocessor.AnalysisAssemblyData = null;
				WithSuppressedLogging( errors, () =>
				{
					CilboxScenePostprocessor.OnPostprocessScene( sh.scene, true );
				} );
				string assemblyData = CilboxScenePostprocessor.AnalysisAssemblyData;
				if( string.IsNullOrEmpty( assemblyData ) ) continue;

				foreach( Type boxType in sh.boxTypes )
				{
					GameObject host = null;
					try
					{
						host = new GameObject( "CilboxCompatAnalyzerHost" ) { hideFlags = HideFlags.HideAndDontSave };
						Cilbox box = host.AddComponent( boxType ) as Cilbox;
						if( box == null ) continue;
						box.assemblyData = assemblyData;
						WithSuppressedLogging( errors, () =>
						{
							box.ForceReinit();
							box.BoxInitialize();
						} );
					}
					catch( Exception e )
					{
						errors.Add( FirstLine( e.Message ) );
					}
					finally
					{
						if( host != null ) UnityEngine.Object.DestroyImmediate( host );
					}
				}
			}

			var unique = new List<string>();
			var seen = new HashSet<string>();
			foreach( string e in errors ) if( seen.Add( e ) ) unique.Add( e );
			return unique;
		}

		static List<KeyValuePair<string, string>> RunContentPoliceCheck( List<SceneHost> sceneHosts )
		{
			var issues = new List<KeyValuePair<string, string>>();
			if( sceneHosts.Count == 0 ) return issues;

			string selectorName;
			HashSet<string> approved = FindWorldContentPoliceApprovals( out selectorName );
			if( approved == null ) return issues;

			CheckSurvives( approved, selectorName, typeof( CilboxProxy ).FullName, "the Cilbox interpreted-script proxy (every cilbox script would be stripped)", issues );
			foreach( SceneHost sh in sceneHosts )
				foreach( Type bt in sh.boxTypes )
					CheckSurvives( approved, selectorName, bt.FullName, "the Cilbox host (the whole sandbox would be stripped)", issues );

			var flagged = new HashSet<string>();
			foreach( SceneHost sh in sceneHosts )
			{
				foreach( GameObject root in sh.scene.GetRootGameObjects() )
				{
					foreach( Component comp in root.GetComponentsInChildren<Component>( true ) )
					{
						if( comp == null ) continue;
						Type ct = comp.GetType();
						if( CilboxUtil.HasCilboxableAttribute( ct ) ) continue;
						string fn = ct.FullName;
						if( string.IsNullOrEmpty( fn ) || approved.Contains( fn ) ) continue;
						if( !flagged.Add( fn ) ) continue;
						issues.Add( new KeyValuePair<string, string>(
							$"component type '{fn}' is not approved by {selectorName} and will be destroyed when the world loads (e.g. on '{GetGameObjectPath( comp.gameObject )}'). Add it to the approved list or remove it.",
							ScriptFrameLine( comp, fn ) ) );
						if( flagged.Count >= 200 ) return issues;
					}
				}
			}
			return issues;
		}

		static string ScriptFrameLine( Component comp, string typeFullName )
		{
			if( !( comp is MonoBehaviour mb ) ) return null;
			MonoScript ms = MonoScript.FromMonoBehaviour( mb );
			string path = ms != null ? AssetDatabase.GetAssetPath( ms ) : null;
			if( string.IsNullOrEmpty( path ) ) return null;

			string simple = typeFullName;
			int dot = simple.LastIndexOf( '.' ); if( dot >= 0 ) simple = simple.Substring( dot + 1 );
			int plus = simple.LastIndexOf( '+' ); if( plus >= 0 ) simple = simple.Substring( plus + 1 );
			return CilboxSourceLog.FrameLineForType( typeFullName, path, FindTypeDeclarationLine( path, simple ) );
		}

		static int FindTypeDeclarationLine( string scriptPath, string simpleName )
		{
			try
			{
				string[] lines = File.ReadAllLines( scriptPath );
				Regex rx = new Regex( @"\b(?:class|struct|interface|enum)\s+" + Regex.Escape( simpleName ) + @"\b" );
				for( int i = 0; i < lines.Length; i++ )
					if( rx.IsMatch( lines[i] ) ) return i + 1;
			}
			catch { }
			return 1;
		}

		static void CheckSurvives( HashSet<string> approved, string selectorName, string typeFullName, string why, List<KeyValuePair<string, string>> issues )
		{
			if( string.IsNullOrEmpty( typeFullName ) || approved.Contains( typeFullName ) ) return;
			issues.Add( new KeyValuePair<string, string>(
				$"REQUIRED type '{typeFullName}' is NOT approved by {selectorName} -- it will be stripped at load, removing {why}.", null ) );
		}

		static HashSet<string> FindWorldContentPoliceApprovals( out string selectorName )
		{
			selectorName = null;
			Type selectorType = FindTypeByName( "ContentPoliceSelector" );
			if( selectorType == null ) return null;

			string[] guids;
			try { guids = AssetDatabase.FindAssets( "t:ContentPoliceSelector" ); }
			catch { return null; }
			if( guids == null || guids.Length == 0 ) return null;

			string bestPath = null; int bestScore = int.MinValue;
			foreach( string guid in guids )
			{
				string path = AssetDatabase.GUIDToAssetPath( guid );
				if( string.IsNullOrEmpty( path ) ) continue;
				string lower = path.ToLowerInvariant();
				int score = 0;
				if( lower.Contains( "scene" ) ) score += 10;
				if( lower.Contains( "world" ) ) score += 9;
				if( lower.Contains( "avatar" ) || lower.Contains( "prop" ) || lower.Contains( "system" ) ) score -= 10;
				if( score > bestScore ) { bestScore = score; bestPath = path; }
			}
			if( bestPath == null ) return null;

			UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath( bestPath, selectorType );
			if( asset == null ) return null;
			FieldInfo f = selectorType.GetField( "selectedTypes", BindingFlags.Public | BindingFlags.Instance );
			if( f == null ) return null;
			var list = f.GetValue( asset ) as System.Collections.IEnumerable;
			if( list == null ) return null;

			var set = new HashSet<string>( StringComparer.Ordinal );
			foreach( object o in list ) { string s = o as string; if( !string.IsNullOrEmpty( s ) ) set.Add( s ); }
			selectorName = "Basis ContentPolice (" + Path.GetFileNameWithoutExtension( bestPath ) + ")";
			return set;
		}

		static Type FindTypeByName( string fullName )
		{
			foreach( Assembly a in AppDomain.CurrentDomain.GetAssemblies() )
			{
				try { Type t = a.GetType( fullName ); if( t != null ) return t; }
				catch { }
			}
			return null;
		}

		static string GetGameObjectPath( GameObject go )
		{
			if( go == null ) return "<null>";
			var sb = new StringBuilder( go.name );
			Transform tr = go.transform.parent;
			int guard = 0;
			while( tr != null && guard++ < 64 ) { sb.Insert( 0, tr.name + "/" ); tr = tr.parent; }
			return sb.ToString();
		}

		static void WithSuppressedLogging( List<string> errors, Action action )
		{
			ILogHandler original = Debug.unityLogger.logHandler;
			var capture = new CaptureLogHandler( errors );
			Debug.unityLogger.logHandler = capture;
			try { action(); }
			catch( Exception e ) { errors.Add( FirstLine( e.Message ) ); }
			finally { Debug.unityLogger.logHandler = original; }
		}

		class CaptureLogHandler : ILogHandler
		{
			readonly List<string> errors;
			public CaptureLogHandler( List<string> e ) { errors = e; }
			public void LogFormat( LogType logType, UnityEngine.Object context, string format, params object[] args )
			{
				if( logType == LogType.Error || logType == LogType.Assert || logType == LogType.Exception )
				{
					string msg;
					try { msg = ( args != null && args.Length > 0 ) ? string.Format( format, args ) : format; }
					catch { msg = format; }
					errors.Add( FirstLine( msg ) );
				}
			}
			public void LogException( Exception exception, UnityEngine.Object context )
			{
				errors.Add( FirstLine( exception.Message ) );
			}
		}

		static string FirstLine( string s )
		{
			if( string.IsNullOrEmpty( s ) ) return s;
			int nl = s.IndexOf( '\n' );
			return nl < 0 ? s.Trim() : s.Substring( 0, nl ).Trim();
		}

		static HashSet<int> sCacheSingle, sCacheTwo;
		static string sCacheHash;

		static bool TryLoadSupportedOpcodes( out HashSet<int> single, out HashSet<int> two, out string error )
		{
			single = null; two = null; error = null;
			string path = FindInterpreterSourcePath();
			if( path == null ) { error = "Cilbox.cs not found"; return false; }
			string src;
			try { src = File.ReadAllText( path ); }
			catch( Exception e ) { error = e.Message; return false; }

			string hash = path + ":" + src.Length;
			if( sCacheHash == hash && sCacheSingle != null ) { single = sCacheSingle; two = sCacheTwo; return true; }

			if( !TryParseSupportedOpcodes( src, out single, out two, out error ) ) return false;

			sCacheHash = hash; sCacheSingle = single; sCacheTwo = two;
			return true;
		}

		static string FindInterpreterSourcePath()
		{
			const string known = "Packages/com.cnlohr.cilbox/Cilbox.cs";
			if( File.Exists( known ) ) return known;
			foreach( string guid in AssetDatabase.FindAssets( "Cilbox t:MonoScript" ) )
			{
				string p = AssetDatabase.GUIDToAssetPath( guid );
				if( p != null && p.EndsWith( "/Cilbox.cs", StringComparison.Ordinal ) && File.Exists( p ) )
					return p;
			}
			return null;
		}

		static readonly Regex sCaseRegex = new Regex( @"^([ \t]*)case\s+0x0*([0-9a-fA-F]{1,2})\s*:", RegexOptions.Compiled );
		static readonly Regex sCaseAllRegex = new Regex( @"case\s+0x([0-9a-fA-F]{1,2})\s*:", RegexOptions.Compiled );
		static readonly Regex sCaseFeRegex = new Regex( @"^[ \t]*case\s+0x0*fe\s*:", RegexOptions.Compiled | RegexOptions.IgnoreCase );

		static bool TryParseSupportedOpcodes( string src, out HashSet<int> single, out HashSet<int> two, out string error )
		{
			single = new HashSet<int>();
			two = new HashSet<int>();
			error = null;

			string[] lines = src.Replace( "\r\n", "\n" ).Replace( "\r", "\n" ).Split( '\n' );

			int mainDefault = IndexOfContains( lines, "Opcode 0x{b.ToString(\"X2\")} unimplemented", 0 );
			int extDefault  = IndexOfContains( lines, "Opcode 0xfe 0x{b.ToString(\"X2\")} unimplemented", 0 );
			int firstNop    = IndexOfRegex( lines, sCaseRegex, 0, 0x00 );
			int extCase     = LastCaseFeBefore( lines, extDefault );

			if( mainDefault < 0 || extDefault < 0 || firstNop < 0 || extCase < 0 )
			{
				error = $"anchors not found (nop={firstNop}, fe={extCase}, extDef={extDefault}, mainDef={mainDefault})";
				return false;
			}

			CollectHexCases( lines, firstNop, mainDefault, extCase, extDefault, single );
			CollectHexCases( lines, extCase + 1, extDefault, -1, -1, two );

			if( single.Count < 80 ||
				!single.Contains( 0x00 ) || !single.Contains( 0x28 ) || !single.Contains( 0x2a ) || !single.Contains( 0x2b ) ||
				!single.Contains( 0x59 ) || !single.Contains( 0x5a ) || !single.Contains( 0x9d ) )
			{ error = $"single-byte set failed sanity (count={single.Count})"; return false; }
			if( two.Count < 7 || !two.Contains( 0x01 ) || !two.Contains( 0x07 ) || !two.Contains( 0x16 ) )
			{ error = $"two-byte set failed sanity (count={two.Count})"; return false; }

			return true;
		}

		static void CollectHexCases( string[] lines, int start, int end, int exclStart, int exclEnd, HashSet<int> outSet )
		{
			for( int k = Math.Max( 0, start ); k < end && k < lines.Length; k++ )
			{
				if( exclStart >= 0 && k >= exclStart && k < exclEnd ) continue;
				string line = lines[k];
				int comment = line.IndexOf( "//", StringComparison.Ordinal );
				if( comment >= 0 ) line = line.Substring( 0, comment );
				foreach( Match mm in sCaseAllRegex.Matches( line ) )
					outSet.Add( Convert.ToInt32( mm.Groups[1].Value, 16 ) );
			}
		}

		static int IndexOfContains( string[] lines, string needle, int from )
		{
			for( int k = from; k < lines.Length; k++ )
				if( lines[k].IndexOf( needle, StringComparison.Ordinal ) >= 0 ) return k;
			return -1;
		}

		static int IndexOfRegex( string[] lines, Regex rx, int from, int requireValue )
		{
			for( int k = from; k < lines.Length; k++ )
			{
				Match m = rx.Match( lines[k] );
				if( m.Success && Convert.ToInt32( m.Groups[2].Value, 16 ) == requireValue ) return k;
			}
			return -1;
		}

		static int LastCaseFeBefore( string[] lines, int before )
		{
			if( before < 0 ) before = lines.Length;
			int found = -1;
			for( int k = 0; k < before && k < lines.Length; k++ )
				if( sCaseFeRegex.IsMatch( lines[k] ) ) found = k;
			return found;
		}

		static string Display( Type t )
		{
			return string.IsNullOrEmpty( t.FullName ) ? t.Name : t.FullName;
		}
	}
}
#endif

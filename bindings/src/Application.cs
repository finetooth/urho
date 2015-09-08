//
// Support for bubbling up to C# the virtual methods calls for Setup, Start and Stop in Application
//
// This is done by using an ApplicationProxy in C++ that bubbles up
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Urho {
	
	public partial class Application {
		readonly ActionIntPtr setup;
		readonly ActionIntPtr start;
		readonly ActionIntPtr stop;
		static readonly object invokerLock = new object();
		static readonly List<Action> invokeOnMain = new List<Action>();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ActionIntPtr (IntPtr value);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int SdlCallback(IntPtr context);

		[DllImport ("mono-urho", CallingConvention=CallingConvention.Cdecl)]
		extern static IntPtr ApplicationProxy_ApplicationProxy (IntPtr contextHandle, ActionIntPtr setup, ActionIntPtr start, ActionIntPtr stop);

		[DllImport("mono-urho", CallingConvention = CallingConvention.Cdecl)]
		extern static void RegisterSdlLauncher(SdlCallback callback);

		[DllImport("mono-urho")]
		extern static void InitSdl(string resDir, string docDir);

		[DllImport("mono-urho", CallingConvention = CallingConvention.Cdecl)]
		extern static void SDL_SetMainReady();

		public static Application Current { get; private set; }

		/// <summary>
		/// Supports the simple style with callbacks
		/// </summary>
		public Application (Context context, ActionIntPtr setup, ActionIntPtr start, ActionIntPtr stop) : base (UrhoObjectFlag.Empty)
		{
			//keep references to callbacks as long as the App is alive
			this.setup = setup;
			this.start = start;
			this.stop = stop;

			if (context == null)
				throw new ArgumentNullException (nameof(context));

			handle = ApplicationProxy_ApplicationProxy (context.Handle, setup, start, stop);
			Runtime.RegisterObject (this);
			Current = this;

			SubscribeToUpdate(args =>
				{
					var timeStep = args.TimeStep;
					Update?.Invoke(args);
					ActionManager.Update(timeStep);
					OnUpdate(timeStep);
				});

			SubscribeToSceneUpdate(args =>
				{
					var timeStep = args.TimeStep;
					var scene = args.Scene;
					SceneUpdate?.Invoke(args);
					OnSceneUpdate(timeStep, scene);
				});
		}

		public Application(Context context) : this(context, ProxySetup, ProxyStart, ProxyStop) { }

		/// <summary>
		/// Should be called in Android and IOS
		/// </summary>
		public static void RegisterSdlLauncher(Func<Application> appFactory)
		{
			RegisterSdlLauncher(_ => appFactory().Run());
		}

		public static void InitializeSdl(string resDir, string docsDir)
		{
			InitSdl(resDir, docsDir);
			SDL_SetMainReady();
		}

		public static event Action<UpdateEventArgs> Update;

		public static event Action<SceneUpdateEventArgs> SceneUpdate;

		static public void InvokeOnMain (Action action)
		{
			lock (invokerLock)
				invokeOnMain.Add (action);
		}

		void RunOnMainThread ()
		{
			lock (invokerLock){
				var count = invokeOnMain.Count;
				if (count > 0){
					foreach (var a in invokeOnMain)
						a ();
					invokeOnMain.Clear ();
				}
			}
		}

		static Application GetApp (IntPtr h)
		{
			return Runtime.LookupObject<Application> (h);
		}
		
		static void ProxySetup (IntPtr h)
		{
			GetApp (h).Setup ();
		}

		static void ProxyStart (IntPtr h)
		{
			GetApp (h).Start ();
		}

		static void ProxyStop (IntPtr h)
		{
			GetApp (h).Stop ();
		}

		public virtual void Setup ()
		{
		}

		public virtual void Start ()
		{
		}

		public virtual void Stop ()
		{
			//Engine.DumpResources(true);
		}

		public ActionManager ActionManager { get; } = new ActionManager();

		protected virtual void OnSceneUpdate(float timeStep, Scene scene) { }

		protected virtual void OnUpdate(float timeStep) { }

		//
		// GetSubsystem helpers
		//
		ResourceCache resourceCache;
		public ResourceCache ResourceCache {
			get {
				if (resourceCache == null)
					resourceCache = new ResourceCache (UrhoObject_GetSubsystem (handle, ResourceCache.TypeStatic.Code));
				return resourceCache;
			}
		}

		UrhoConsole console;
		public UrhoConsole Console {
			get {
				if (console == null)
					console = new UrhoConsole (UrhoObject_GetSubsystem (handle, UrhoConsole.TypeStatic.Code));
				return console;
			}
		}
		
		Network network;
		public Network Network {
			get {
				if (network == null)
					network = new Network (UrhoObject_GetSubsystem (handle, Network.TypeStatic.Code));
				return network;
			}
		}
		
		Time time;
		public Time Time {
			get {
				if (time == null)
					time = new Time (UrhoObject_GetSubsystem (handle, Time.TypeStatic.Code));
				return time;
			}
		}
		
		WorkQueue workQueue;
		public WorkQueue WorkQueue {
			get {
				if (workQueue == null)
					workQueue = new WorkQueue (UrhoObject_GetSubsystem (handle, WorkQueue.TypeStatic.Code));
				return workQueue;
			}
		}
		
		Profiler profiler;
		public Profiler Profiler {
			get {
				if (profiler == null)
					profiler = new Profiler (UrhoObject_GetSubsystem (handle, Profiler.TypeStatic.Code));
				return profiler;
			}
		}
		
		FileSystem fileSystem;
		public FileSystem FileSystem {
			get {
				if (fileSystem == null)
					fileSystem = new FileSystem (UrhoObject_GetSubsystem (handle, FileSystem.TypeStatic.Code));
				return fileSystem;
			}
		}
		
		Log log;
		public Log Log {
			get {
				if (log == null)
					log = new Log (UrhoObject_GetSubsystem (handle, Log.TypeStatic.Code));
				return log;
			}
		}
		
		Input input;
		public Input Input {
			get {
				if (input == null)
					input = new Input (UrhoObject_GetSubsystem (handle, Input.TypeStatic.Code));
				return input;
			}
		}
		
		Audio audio;
		public Audio Audio {
			get {
				if (audio == null)
					audio = new Audio (UrhoObject_GetSubsystem (handle, Audio.TypeStatic.Code));
				return audio;
			}
		}
		
		UI uI;
		public UI UI {
			get {
				if (uI == null)
					uI = new UI (UrhoObject_GetSubsystem (handle, UI.TypeStatic.Code));
				return uI;
			}
		}
		
		Graphics graphics;
		public Graphics Graphics {
			get {
				if (graphics == null)
					graphics = new Graphics (UrhoObject_GetSubsystem (handle, Graphics.TypeStatic.Code));
				return graphics;
			}
		}
		
		Renderer renderer;
		public Renderer Renderer {
			get {
				if (renderer == null)
					renderer = new Renderer (UrhoObject_GetSubsystem (handle, Renderer.TypeStatic.Code));
				return renderer;
			}
		}

		[DllImport ("mono-urho", CallingConvention=CallingConvention.Cdecl)]
		extern static IntPtr Application_GetEngine (IntPtr handle);
		Engine engine;
		public Engine Engine {
			get {
				if (engine == null)
					engine = new Engine (Application_GetEngine (handle));
				return engine;
			}
		}
	}
}

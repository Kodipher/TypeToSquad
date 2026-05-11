using Godot;


namespace TypeToSquad.Gui;


public partial class EntryPoint : Node {
	
	public override void _Ready() {
		CallDeferred(MethodName.PostReady);
	}

	void PostReady() {
		WindowManager.Instance.CreateWindowIntoRoot(WindowType.Main);
	}

}

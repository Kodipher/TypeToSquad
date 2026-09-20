using Godot;
using System;


namespace TypeToSquad.Gui;


public partial class WindowSelectionPopup : PopupMenu {
	
	[Export] public Godot.Collections.Dictionary<long, WindowType> ItemIdToWindowType { get; set; } = [];

	public override void _Ready() {
		this.IdPressed += OnIdPressed;
	}

	public void OnIdPressed(long id) {
		
		if (!ItemIdToWindowType.TryGetValue(id, out WindowType windowType)) {
			throw new InvalidOperationException($"Item with ID {id} has no window type assigned.");
		}

		WindowManager.Instance.CreateWindowAtSelfUnique(windowType);
	}
	
}

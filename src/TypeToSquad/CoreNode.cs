using Godot;
using System;
using System.Collections.Generic;


namespace TypeToSquad;


public interface IRefrencesCore {
	public void RecieveCoreReference(CoreNode core);
}


public partial class CoreNode : Node {

	public override void _Ready() {
		base._Ready();

	}

	#region //// Children

	#endregion

}

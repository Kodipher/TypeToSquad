using Godot;
using System;

using TypeToSquad.Model;
using TypeToSquad.Utils;
using TypeToSquad.Model.Markup;


namespace TypeToSquad.Gui.WindowScenes;


public partial class RawSsmlInputWindow : Window {
	
	CodeEdit codeEdit = null!;
	
	public override void _Ready() {
		base._Ready();

		// Closing
		this.CloseRequested += OnClose;

		var closeButton = this.GetNodeNotNull<BaseButton>("%CloseButton");
		closeButton.Pressed += OnClose;
		
		// Find main text edit
		codeEdit = this.GetNodeNotNull<CodeEdit>("%CodeEdit");
		OnResetPressed();
		
		// Init buttons
		var speakButton = this.GetNodeNotNull<BaseButton>("%SpeakButton");
		var shutButton = this.GetNodeNotNull<BaseButton>("%ShutButton");
		var importButton = this.GetNodeNotNull<BaseButton>("%ImportButton");
		var resetButton = this.GetNodeNotNull<BaseButton>("%ResetButton");

		speakButton.Pressed += OnSpeakPressed;
		shutButton.Pressed += OnShutPressed;
		importButton.Pressed += OnImportPressed;
		resetButton.Pressed += OnResetPressed;
	}
	
	public void OnClose() {
		GD.Print("Closing Raw SSML Input");
		this.QueueFree();
	}
	
	public void OnSpeakPressed() {
		GD.Print("Synthesizing [Raw SSML]...");
		AudioProvider.Instance.CreateStreamFromTextOrSsml(
			codeEdit.Text,
			isSsml: true,
			stream => {
				GD.Print("Playing [Raw SSML]...");
				AudioManager.Instance.PlayNew(stream);
			}
		);
	}

	public void OnShutPressed() {
		GD.Print("Shutting [Raw SSML].");
		AudioManager.Instance.StopAll();
	}

	public void OnResetPressed() {
		var root = MessageProcessor.ProcessMessage("Hello, World!");
		root = MessageProcessor.WrapInSsmlIfPlainText(root);
		codeEdit.Text = MessageProcessor.StringifyNodeRecursive(root, indented: true).Trim();
	}

	public void OnImportPressed() {
		
		var appRoot = WindowManager.Instance.GetWindow();
		if (appRoot is not MainWindow mainWindow) {
			throw new InvalidOperationException($"App root is not {nameof(MainWindow)}");
		}

		string mainMessageInputString = mainWindow.MessageTextEdit.Text;
		
		GD.Print("Processing and importing [Raw SSML]...");
		var root = MessageProcessor.ProcessMessage(mainMessageInputString);
		root = MessageProcessor.WrapInSsmlIfPlainText(root);
		codeEdit.Text = MessageProcessor.StringifyNodeRecursive(root, indented: true).Trim();
	}
	
}

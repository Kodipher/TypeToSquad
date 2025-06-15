using Godot;
using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;


namespace TypeToSquad.Scenes;


public partial class MainScene : HBoxContainer {

	Button testButton;
	TextEdit testTextEdit;

	NamedPipeClientStream pipeClientStream;
	StreamReader reader;
	StreamWriter writer;

	public override void _Ready() {
		testButton = GetNode<Button>("Button");
		testTextEdit = GetNode<TextEdit>("Label");
		testButton.Pressed += PipeExchange;

		
	}


	public override void _Process(double delta) {
	}

	public void PipeExchange() {
		pipeClientStream = new NamedPipeClientStream(".", "TESTPIPE", PipeDirection.InOut, PipeOptions.Asynchronous);
		reader = new StreamReader(pipeClientStream);
		writer = new StreamWriter(pipeClientStream);
		pipeClientStream.Connect(5000);
		writer.WriteLine(testTextEdit.Text);
		writer.Flush();
		testTextEdit.Text = reader.ReadLine();
		pipeClientStream.Dispose();
	}

}

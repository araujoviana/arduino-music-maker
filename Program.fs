open System
open System.IO
open System.IO.Ports
open System.Threading
open Spectre.Console

// TODO add boundaries
let getNoteFrequency position =
    let baseFrequency: float = 440.0 // A4
    let basePosition: int = 49

    let frequency = baseFrequency * 2.0 ** ((float (position - basePosition)) / 12.0)
    System.Math.Round(frequency) |> int


let callArduino (serialPort: SerialPort) (note: int) (durationMs: int) = // Modified to take SerialPort object
    try
        AnsiConsole.WriteLine($"Sending note: {note} for {durationMs}ms")
        serialPort.WriteLine(string note)
        Thread.Sleep(durationMs)
        AnsiConsole.WriteLine("Note sent successfully.")
    with ex ->
        AnsiConsole.MarkupLine($"[red]Error sending note: {ex.Message}[/]")


// TODO Rename
// TODO Make it generic instead of only returning a string
let promptUserInfo promptMessage (defaultValue: string) validationFun : string =
    AnsiConsole.Prompt(
        (new TextPrompt<string>(promptMessage)).DefaultValue(defaultValue).Validate(fun input -> validationFun input)
    )


[<EntryPoint>]
let main argv =
    AnsiConsole.MarkupLine("[underline red]BPM![/]")

    let portName: string =
        promptUserInfo "Arduino port" "/dev/ttyUSB0" (fun (p: string) ->
            try
                use port = new SerialPort(p)
                port.Open()
                port.Close()
                ValidationResult.Success()
            with ex ->
                ValidationResult.Error("[red]Couldn't connect to port[/]"))

    let baudRate: int =
        promptUserInfo "Baud rate" "9600" (fun baudStr ->
            match Int32.TryParse(baudStr) with
            | (true, baud) when baud > 0 -> ValidationResult.Success()
            | (_, _) -> ValidationResult.Error("[red]Baud rate must be a positive integer.[/]"))
        |> Int32.Parse

    let bpm =
        promptUserInfo "BPM (Beats Per Minute)" "120" (fun bpmStr ->
            match Int32.TryParse(bpmStr) with
            | (true, bpm) when bpm > 0 -> ValidationResult.Success()
            | (_, _) -> ValidationResult.Error("[red]BPM must be a positive integer."))

    let beatDurationMs = 60000.0 / (float bpm)

    AnsiConsole.MarkupLine($"[blue]Beat duration: {beatDurationMs}[/]")

    let melody =
        [ (getNoteFrequency 49, 1.0)
          (getNoteFrequency 52, 0.5)
          (getNoteFrequency 52, 0.5)
          (getNoteFrequency 49, 1.0) ]

    AnsiConsole.WriteLine("Playing melody...")


    use serialPort = new SerialPort(portName, baudRate)
    serialPort.Open()
    Thread.Sleep(1000)

    for (noteFreq, duration) in melody do
        let noteDurationMs = int (beatDurationMs * duration)
        callArduino serialPort noteFreq noteDurationMs

    serialPort.Close()

    AnsiConsole.WriteLine("Done!")
    0

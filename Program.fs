// TODO Convert docstrings to XML documentation

open System
open System.IO
open System.IO.Ports
open System.Threading
open Spectre.Console

// TODO add boundaries
let getNoteFrequency position =
    let baseFrequency: float = 440.0 // frequency for the base note (A4)
    let basePosition: int = 49 // MIDI position for the base note

    baseFrequency * 2.0 ** ((float (position - basePosition)) / 12.0)
    |> System.Math.Round
    |> int

/// Sends a musical note to the specified serial port for a given duration.
let sendNote (serialPort: SerialPort) (note: int) (durationMs: int) =
    try
        AnsiConsole.MarkupLine($"Sending note: [blue]{note} Hz[/] for [green]{durationMs}ms[/]")

        serialPort.WriteLine(string note)

        Thread.Sleep(durationMs) // Delay between notes

    with ex ->
        AnsiConsole.MarkupLine($"[red]Error sending note: {ex.Message}[/]")


// TODO Add exception handling
/// Plays a song by sending notes to Arduino through serial communication.
let playSong serialPort baudRate beatDurationMs melody =

    AnsiConsole.WriteLine("Playing music!")

    use serialPort = new SerialPort(serialPort, baudRate)
    serialPort.Open()

    Thread.Sleep(1000) // Waits until arduino is done setting up -> TODO could be customizable

    // Iterate through the melody (list of tuples: (note frequency, note duration in beats))
    for (noteFreq, duration) in melody do
        let noteDurationMs = int (beatDurationMs * duration)
        sendNote serialPort noteFreq noteDurationMs

    serialPort.Close()

    AnsiConsole.WriteLine("Done!")


// TODO Make it generic instead of only returning a string
let promptValue promptMessage (defaultValue: string) validationFun : string =
    AnsiConsole.Prompt(
        (new TextPrompt<string>(promptMessage)).DefaultValue(defaultValue).Validate(fun input -> validationFun input)
    )


[<EntryPoint>]
let main argv =
    AnsiConsole.MarkupLine("[cyan]Arduino[/] Music Maker!")

    // Arduino serial port
    let portName: string =
        promptValue "Arduino port" "/dev/ttyUSB0" (fun (p: string) ->
            // Checks for an available port
            try
                use port = new SerialPort(p)
                port.Open()
                port.Close()
                ValidationResult.Success()
            with ex ->
                ValidationResult.Error("[red]Couldn't connect to port[/]"))

    // Baud rate
    let baudRate: int =
        promptValue "Baud rate" "9600" (fun baudStr ->
            // Checks for a positive integer
            match Int32.TryParse(baudStr) with
            | (true, baud) when baud > 0 -> ValidationResult.Success()
            | (_, _) -> ValidationResult.Error("[red]Baud rate must be a positive integer.[/]"))
        |> Int32.Parse

    // Beats per minute
    let bpm =
        promptValue "BPM (Beats Per Minute)" "120" (fun bpmStr ->
            // Checks for a positive integer
            match Int32.TryParse(bpmStr) with
            | (true, bpm) when bpm > 0 -> ValidationResult.Success()
            | (_, _) -> ValidationResult.Error("[red]BPM must be a positive integer."))

    // Duration of one beat in milliseconds
    let beatDurationMs = 60000.0 / (float bpm)

    AnsiConsole.MarkupLine($"[blue]Beat duration: {beatDurationMs}[/]")

    // TODO make the user write their own
    // Example song written by ChatGPT because i'm not a musician.
    let melody =
        [ (getNoteFrequency 36, 1.0) // C3 - Quarter note
          (getNoteFrequency 36, 1.0) // C3 - Quarter note
          (getNoteFrequency 38, 0.5) // D3 - Eighth note
          (getNoteFrequency 38, 0.5) // D3 - Eighth note
          (getNoteFrequency 40, 1.0) // E3 - Quarter note
          (getNoteFrequency 40, 1.0) // E3 - Quarter note

          (getNoteFrequency 41, 0.5) // F3 - Eighth note
          (getNoteFrequency 41, 0.5) // F3 - Eighth note
          (getNoteFrequency 43, 0.5) // G3 - Eighth note
          (getNoteFrequency 43, 0.5) // G3 - Eighth note
          (getNoteFrequency 45, 1.0) // A3 - Quarter note
          (getNoteFrequency 45, 1.0) // A3 - Quarter note

          (getNoteFrequency 43, 0.5) // G3 - Eighth note
          (getNoteFrequency 43, 0.5) // G3 - Eighth note
          (getNoteFrequency 41, 0.5) // F3 - Eighth note
          (getNoteFrequency 41, 0.5) // F3 - Eighth note
          (getNoteFrequency 40, 1.0) // E3 - Quarter note
          (getNoteFrequency 40, 1.0) // E3 - Quarter note

          (getNoteFrequency 38, 0.5) // D3 - Eighth note
          (getNoteFrequency 38, 0.5) // D3 - Eighth note
          (getNoteFrequency 36, 1.0) // C3 - Quarter note
          (getNoteFrequency 36, 1.0) // C3 - Quarter note

          // Variation (more rhythm emphasis)
          (getNoteFrequency 43, 0.25) // G3 - Sixteenth note
          (getNoteFrequency 45, 0.25) // A3 - Sixteenth note
          (getNoteFrequency 47, 0.25) // B3 - Sixteenth note
          (getNoteFrequency 45, 0.25) // A3 - Sixteenth note
          (getNoteFrequency 43, 0.5) // G3 - Eighth note
          (getNoteFrequency 41, 0.5) // F3 - Eighth note
          (getNoteFrequency 40, 1.0) // E3 - Quarter note

          (getNoteFrequency 38, 0.5) // D3 - Eighth note
          (getNoteFrequency 36, 1.0) ] // C3 - Quarter note

    playSong portName baudRate beatDurationMs melody

    0

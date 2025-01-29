// TODO Convert docstrings to XML documentation

open System
open System.IO
open System.IO.Ports
open System.Threading
open Spectre.Console


// TODO Make it generic instead of only returning a string
let promptValue promptMessage (defaultValue: string) validationFun : string =
    AnsiConsole.Prompt(
        (new TextPrompt<string>(promptMessage)).DefaultValue(defaultValue).Validate(fun input -> validationFun input)
    )

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
let playSong serialPort baudRate bpm melody =

    // Duration of one beat in milliseconds
    let beatDurationMs = 60000.0 / (float bpm)

    AnsiConsole.MarkupLine($"[blue]Beat duration: {beatDurationMs}[/]")

    use serialPort = new SerialPort(serialPort, baudRate)
    serialPort.Open()

    Thread.Sleep(1000) // Waits until arduino is done setting up -> TODO could be customizable

    // Iterate through the melody (list of tuples: (note frequency, note duration in beats))
    for (noteFreq, duration) in melody do
        let noteDurationMs = int (beatDurationMs * duration)
        sendNote serialPort noteFreq noteDurationMs

    serialPort.Close()


let rec writeSong song =
    let note = promptValue "[green3_1]Note (1-128):[/]" "49" (fun note ->
        // Check for positive integer
        match Int32.TryParse(note) with
        | (true, note) when note > 0 && note <= 128 -> ValidationResult.Success()
        | (false, _) when note = "exit" -> ValidationResult.Success() 
        | _ -> ValidationResult.Error("[red]Note must be between 1 and 128.[/]"))

    if note = "exit" then
        song
    else
        let duration = promptValue "[orange3]Duration:[/]" "1" (fun duration ->
            // Check for positive integer
            match Double.TryParse(duration) with
            | (true, duration) when duration > 0.0 && duration < 1000.0 -> ValidationResult.Success()
            | (false, _) when duration = "exit" -> ValidationResult.Success() 
            | _ -> ValidationResult.Error("[red]Duration must be a positive integer.[/]"))

        if duration = "exit" then
            song
        else
            let updatedSong = (int note |> getNoteFrequency, float duration) :: song
            writeSong updatedSong




let sampleSong =
    // TODO make the user write their own
    // Example song written by ChatGPT because i'm not a musician.
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




let setConnectionConfig =

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

    portName, baudRate

let setBPM =
    promptValue "BPM (Beats Per Minute)" "120" (fun bpmStr ->
        // Checks for a positive integer
        match Int32.TryParse(bpmStr) with
        | (true, bpm) when bpm > 0 -> ValidationResult.Success()
        | (_, _) -> ValidationResult.Error("[red]BPM must be a positive integer."))



[<EntryPoint>]
let main argv =
    // TODO Make this text appear at the beginning since the others have priority?
    // AnsiConsole.MarkupLine("[cyan]Arduino[/] Music Maker!")

    let initialPortName, initialBaudRate = setConnectionConfig
    let initialBPM = setBPM

    AnsiConsole.MarkupLine("Write [underline red]exit[/] anywhere to finish.")
    let song = writeSong [] |> List.rev
    playSong initialPortName initialBaudRate initialBPM song

    0

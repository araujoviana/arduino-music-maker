open System
open System.IO.Ports
open System.Threading
open System.Runtime.InteropServices
open Spectre.Console

/// <summary>
/// A map associating note names with their corresponding MIDI positions.
/// </summary>
let noteMap =
    [ "C0", 12
      "C#0", 13
      "D0", 14
      "D#0", 15
      "E0", 16
      "F0", 17
      "F#0", 18
      "G0", 19
      "G#0", 20
      "A0", 21
      "A#0", 22
      "B0", 23
      "C1", 24
      "C#1", 25
      "D1", 26
      "D#1", 27
      "E1", 28
      "F1", 29
      "F#1", 30
      "G1", 31
      "G#1", 32
      "A1", 33
      "A#1", 34
      "B1", 35
      "C2", 36
      "C#2", 37
      "D2", 38
      "D#2", 39
      "E2", 40
      "F2", 41
      "F#2", 42
      "G2", 43
      "G#2", 44
      "A2", 45
      "A#2", 46
      "B2", 47
      "C3", 48
      "C#3", 49
      "D3", 50
      "D#3", 51
      "E3", 52
      "F3", 53
      "F#3", 54
      "G3", 55
      "G#3", 56
      "A3", 57
      "A#3", 58
      "B3", 59
      "C4", 60
      "C#4", 61
      "D4", 62
      "D#4", 63
      "E4", 64
      "F4", 65
      "F#4", 66
      "G4", 67
      "G#4", 68
      "A4", 69
      "A#4", 70
      "B4", 71
      "C5", 72
      "C#5", 73
      "D5", 74
      "D#5", 75
      "E5", 76
      "F5", 77
      "F#5", 78
      "G5", 79
      "G#5", 80
      "A5", 81
      "A#5", 82
      "B5", 83
      "C6", 84
      "C#6", 85
      "D6", 86
      "D#6", 87
      "E6", 88
      "F6", 89
      "F#6", 90
      "G6", 91
      "G#6", 92
      "A6", 93
      "A#6", 94
      "B6", 95
      "C7", 96
      "C#7", 97
      "D7", 98
      "D#7", 99
      "E7", 100
      "F7", 101
      "F#7", 102
      "G7", 103
      "G#7", 104
      "A7", 105
      "A#7", 106
      "B7", 107
      "C8", 108 ]
    |> Map.ofList

/// <summary>
/// Gets the MIDI position of a note from the noteMap.
/// </summary>
/// <param name="note">The name of the note.</param>
/// <returns>The MIDI position of the note, or -1 if the note is invalid.</returns>
let getNotePosition note =
    match Map.tryFind note noteMap with
    | Some pos -> pos
    | None -> -1

/// <summary>
/// Calculates the frequency of a note given its MIDI position.
/// </summary>
/// <param name="position">The MIDI position of the note.</param>
/// <returns>The frequency of the note.</returns>
let getNoteFrequency position =
    let baseFrequency: float = 440.0 // frequency for the base note (A4)
    let basePosition: int = 69 // MIDI position for the base note

    baseFrequency * 2.0 ** ((float (position - basePosition)) / 12.0)
    |> System.Math.Round
    |> int

/// <summary>
/// Prompts the user for a value with validation.
/// </summary>
/// <param name="promptMessage">The prompt message.</param>
/// <param name="defaultValue">The default value.</param>
/// <param name="validationFun">The validation function.</param>
/// <returns>The validated user input.</returns>
let promptValue promptMessage (defaultValue: string) validationFun : string =
    AnsiConsole.Prompt(
        (new TextPrompt<string>(promptMessage)).DefaultValue(defaultValue).Validate(fun input -> validationFun input)
    )

/// <summary>
/// Configures the serial port connection.
/// </summary>
/// <returns>A tuple containing the port name and baud rate.</returns>
let setConnectionConfig =
    
    let defaultPort = 
        match RuntimeInformation.IsOSPlatform(OSPlatform.Windows) with
        | true -> "COM0"
        | _ -> "/dev/ttyUSB0"

    // Arduino serial port
    let portName: string =
        promptValue "Arduino port" defaultPort  (fun (p: string) ->
            // Checks for an available port
            try
                use port = new SerialPort(p)
                port.Open()
                port.Close()
                ValidationResult.Success()
            with ex ->
                ValidationResult.Error($"[red]Couldn't connect to port: {ex.Message}[/]"))

    // Baud rate
    let baudRate: int =
        promptValue "Baud rate" "9600" (fun baudStr ->
            // Checks for a positive integer
            match Int32.TryParse(baudStr) with
            | (true, baud) when baud > 0 -> ValidationResult.Success()
            | (_, _) -> ValidationResult.Error("[red]Baud rate must be a positive integer.[/]"))
        |> Int32.Parse

    portName, baudRate

/// <summary>
/// Prompts the user for the BPM (Beats Per Minute).
/// </summary>
/// <returns>The BPM value.</returns>
let setBPM =
    promptValue "BPM (Beats Per Minute)" "120" (fun bpmStr ->
        // Checks for a positive integer
        match Int32.TryParse(bpmStr) with
        | (true, bpm) when bpm > 0 -> ValidationResult.Success()
        | (_, _) -> ValidationResult.Error("[red]BPM must be a positive integer.[/]"))
    |> Int32.Parse


/// <summary>
/// Composes a song by prompting the user for notes and durations.
/// </summary>
/// <returns>A list of tuples representing the song's melody (frequency, duration).</returns>
let composeSong () =
    let song = ResizeArray()
    let mutable note = ""

    while note <> "exit" do
        note <-
            promptValue "[green3_1]Note (e.g., B1, D#3):[/]" "A4" (fun note ->
                let position = note.Trim().ToUpper() |> getNotePosition

                if position <> -1 then ValidationResult.Success()
                elif note = "exit" then ValidationResult.Success()
                else ValidationResult.Error("[red]Invalid note name.[/]"))

        if note <> "exit" then
            let duration =
                promptValue "[orange3]Beat duration:[/]" "1" (fun duration ->
                    match Double.TryParse(duration) with
                    | (true, duration) when duration > 0.0 && duration < 1000.0 -> ValidationResult.Success()
                    | (false, _) when duration = "exit" -> ValidationResult.Success()
                    | _ -> ValidationResult.Error("[red]Duration must be a positive number.[/]"))

            if duration <> "exit" then
                let position = getNotePosition note
                song.Add((position |> getNoteFrequency, float duration))

    song |> Seq.toList


/// <summary>
/// Sends a musical note to the specified serial port for a given duration.
/// </summary>
/// <param name="serialPort">The serial port.</param>
/// <param name="note">The note frequency in Hz.</param>
/// <param name="durationMs">The note duration in milliseconds.</param>
let sendNote (serialPort: SerialPort) (note: int) (durationMs: int) =
    try
        AnsiConsole.MarkupLine($"Sending note: [blue]{note} Hz[/] for [green]{durationMs}ms[/]")
        serialPort.WriteLine(string note)
        Thread.Sleep(durationMs)
    with ex ->
        AnsiConsole.MarkupLine($"[red]Error sending note: {ex.Message}[/]")


/// <summary>
/// Plays a song by sending notes to Arduino through serial communication.
/// </summary>
/// <param name="portName">The serial port name.</param>
/// <param name="baudRate">The baud rate.</param>
/// <param name="bpm">The BPM (Beats Per Minute).</param>
/// <param name="melody">The song melody as a list of (frequency, duration) tuples.</param>
let playSong portName baudRate bpm melody =
    let beatDurationMs = 60000.0 / (float bpm)
    AnsiConsole.MarkupLine($"[blue]Beat duration: {beatDurationMs} ms[/]")

    try
        use serialPort = new SerialPort(portName, baudRate)
        serialPort.Open()
        Thread.Sleep(2000) // Waits until Arduino is done setting up

        for (noteFreq, duration) in melody do
            let noteDurationMs = int (beatDurationMs * duration)
            sendNote serialPort noteFreq noteDurationMs

    with ex ->
        AnsiConsole.MarkupLine($"[red]Error during playback: {ex.Message}[/]")


[<EntryPoint>]
let rec main argv =
    let portName, baudRate = setConnectionConfig
    let bpm = setBPM

    AnsiConsole.MarkupLine("[bold green]Compose your song![/]")
    AnsiConsole.MarkupLine("Write [underline red]exit[/] anywhere to finish.")
    let song = composeSong ()

    AnsiConsole.Status().Start("Playing song...", fun ctx -> playSong portName baudRate bpm song)

    if
        AnsiConsole.Prompt(
            TextPrompt<bool>("Run again?")
                .AddChoice(true)
                .AddChoice(false)
                .DefaultValue(true)
                .WithConverter(fun choice -> if choice then "y" else "n")
        )
    then
        main argv
    else
        0

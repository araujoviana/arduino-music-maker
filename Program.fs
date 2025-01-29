// TODO Convert docstrings to XML documentation

open System
open System.IO.Ports
open System.Threading
open Spectre.Console

// TODO Refactor ?
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

// Function to get the position of a note
let getNotePosition note =
    match Map.tryFind note noteMap with
    | Some pos -> pos
    | None -> -1 // Invalid note

let getNoteFrequency position =
    let baseFrequency: float = 440.0 // frequency for the base note (A4)
    let basePosition: int = 69 // MIDI position for the base note

    baseFrequency * 2.0 ** ((float (position - basePosition)) / 12.0)
    |> System.Math.Round
    |> int

let promptValue promptMessage (defaultValue: string) validationFun : string =
    AnsiConsole.Prompt(
        (new TextPrompt<string>(promptMessage)).DefaultValue(defaultValue).Validate(fun input -> validationFun input)
    )

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

let rec composeSong song =
    let note =
        promptValue "[green3_1]Note (e.g., B1, D#3):[/]" "A4" (fun note ->
            // Check for valid note
            let position = getNotePosition note

            if position <> -1 then ValidationResult.Success()
            elif note = "exit" then ValidationResult.Success()
            else ValidationResult.Error("[red]Invalid note name.[/]"))

    if note = "exit" then
        song
    else
        let duration =
            promptValue "[orange3]Beat duration:[/]" "1" (fun duration ->
                // Check for positive duration
                match Double.TryParse(duration) with
                | (true, duration) when duration > 0.0 && duration < 1000.0 -> ValidationResult.Success()
                | (false, _) when duration = "exit" -> ValidationResult.Success()
                | _ -> ValidationResult.Error("[red]Duration must be a positive number.[/]"))

        if duration = "exit" then
            song
        else
            let position = getNotePosition note
            let updatedSong = (position |> getNoteFrequency, float duration) :: song
            composeSong updatedSong

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





[<EntryPoint>]
let main argv =
    // TODO Make this text appear at the beginning since the others have priority?
    // AnsiConsole.MarkupLine("[cyan]Arduino[/] Music Maker!")

    let portName, baudRate = setConnectionConfig
    let bpm = setBPM

    AnsiConsole.MarkupLine("[bold green]Compose your song![/]")
    AnsiConsole.MarkupLine("Write [underline red]exit[/] anywhere to finish.")
    let song = composeSong [] |> List.rev

    AnsiConsole.Status().Start("Playing song...", fun ctx -> playSong portName baudRate bpm song)

    0

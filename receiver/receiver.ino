#define BUZZER_PIN 9 // HAS to be the same as the one provided in the F# code!
#define BAUD_RATE 9600 // HAS to be the same as the one provided in the F# code!
#define BASE_UNIT 1000 // Minimum duration a note plays
#define TONE_PAUSE 50 // Predefined pause between each note

void setup() {
  Serial.begin(BAUD_RATE);
  pinMode(BUZZER_PIN, OUTPUT);

  pinMode(LED_BUILTIN, OUTPUT); // For debugging purposes


}

void loop() {
  if (Serial.available() > 0) {
    int note = Serial.parseInt();
    if (note > 0) {
      digitalWrite(LED_BUILTIN, HIGH);

      tone(BUZZER_PIN, note, BASE_UNIT);
      delay(TONE_PAUSE);
      noTone(BUZZER_PIN);

      digitalWrite(LED_BUILTIN, LOW);
    }
    while(Serial.available() > 0) {
      Serial.read();
    }
  }
}
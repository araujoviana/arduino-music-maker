#include "config.h"

void setup() {
  Serial.begin(BAUD_RATE);

  pinMode(BUZZER_PIN, OUTPUT);
  pinMode(LED_BUILTIN, OUTPUT); // For debugging purposes
}

void loop() {
  if (Serial.available() > 0) { // Check if there is any serial data available
    int note = Serial.parseInt();
    if (note > 0) {
      digitalWrite(LED_BUILTIN, HIGH);

      tone(BUZZER_PIN, note, BASE_UNIT); // Plays a note with the received frequency
      delay(TONE_PAUSE);

      // noTone(BUZZER_PIN);

      digitalWrite(LED_BUILTIN, LOW);
    }
    // Clear remaining data in the serial buffer
    while(Serial.available() > 0) { 
      Serial.read();
    }
  }
}
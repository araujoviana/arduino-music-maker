// Change according to usage
#define BAUD_RATE 9600
#define BUZZER_PIN 9

int input = 0; // TODO Rename

void setup()
{
    Serial.begin(BAUD_RATE); // CHANGE IF NECESSARY

    pinMode(BUZZER_PIN, OUTPUT);

    Serial.println("Arduino is ready!");
}

void loop() {
    if (Serial.available() > 0) {
        input = Serial.read();

    }
}
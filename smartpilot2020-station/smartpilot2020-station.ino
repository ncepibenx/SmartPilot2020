#include <MOS.h>
#include <SPI.h>
#include <nRF24L01.h>
#include <RF24.h>

RF24 radio(7, 8); // CE, CSN
const byte address[6] = "00001";

void setup() {
  Serial.setTimeout(5);
  Serial.begin(115200);

  // Radio
  radio.begin();
  radio.openReadingPipe(0, address);
  radio.setPALevel(RF24_PA_MIN);
  radio.startListening();
}

void ReceiveTask(PTCB tcb)
{
  MOS_Continue(tcb);

  while(radio.available())
  {
      char telemetry[32];
      radio.read(&telemetry, sizeof(telemetry));
      String telemetryString(telemetry);
    
      Serial.println(telemetryString);
  }
}

void SendControlSignalTask(PTCB tcb)
{
  MOS_Continue(tcb);

  while(Serial.available())
  {
    String flightParams = Serial.readString();
    char copy[32];
    flightParams.toCharArray(copy, 32);

    radio.stopListening();
    radio.openWritingPipe(address);
    radio.write(&copy, 32);
    radio.openReadingPipe(0, address);
    radio.startListening();
  }
}

void StationaryMeasurementTask(PTCB tcb)
{
  MOS_Continue(tcb);

  while(1)
  {
    // StationaryEnvironmentPacket - ID;TEMPERATURE;HUMIDITY;PRESSURE
    Serial.println("5;18,25;44,14;1008");

    Serial.print("6;");
    Serial.println(radio.testCarrier());

    MOS_Delay(tcb, 5000);
  }
}

void ExampleInputTask(PTCB tcb)
{
  MOS_Continue(tcb);

  while(1)
  {
    // RemoteTelemetryPacket - ID;PITCH;ROLL;HEADING;SPEED;ALTITUDE
    Serial.print("2;");
    Serial.print("8");
    Serial.print(";");
    Serial.print("0");
    Serial.print(";");
    Serial.print("250");
    Serial.print(";");
    Serial.print(random(8, 15));
    Serial.print(";");
    Serial.println(random(3, 8));

    // RemotePositionPacket - ID;LATITUDE;LONGITUDE
    Serial.print("3;-97,821");
    Serial.print(random(0, 999));
    Serial.print(";30,239");
    Serial.println(random(0, 999));

    // RemoteEnvironmentPacket - ID;TEMPERATURE;HUMIDITY;PRESSURE
    Serial.println("4;19,25;35,14;1002");

    MOS_Delay(tcb, 500);
  }
}

void loop() {
  MOS_Call(ReceiveTask);
  MOS_Call(SendControlSignalTask);
  MOS_Call(StationaryMeasurementTask);
  //MOS_Call(ExampleInputTask);
}

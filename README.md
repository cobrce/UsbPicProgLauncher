# UsbPicProgLauncher
A program that monitor usb devices, when UsbPicProg programmer is plugged in, its software is launched
Basically you can monitor any device and launch any program just by modifiying profiles.json

Newtonsoft.Json.11.0.2 is required

#### How to use
just run the program, if setting.json is not found it will automatically create it, no window but a message box is shown , it runs in the background

#### profiles.json
it's a json file with the following format
```
{
  DeviceName : AbsolutePathOfThePrograToRunWhenDetected,
  AnotherDevice : AnotherProgram
}
```
The above vars are self explanatory, you can add your own Device:Program pair following the same pattern, dont forget to use `\\` instead of `\` for program path

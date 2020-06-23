# The kodu-microbit Project

This project builds the `kodu-microbit-combined.hex` file. This is the image Kodu flashes to attached microbits when entering play mode.

## Prerequisites (Windows)
1. Install SRecord
    
    http://srecord.sourceforge.net/
    
    Unzip to a local folder and add it to your PATH.

2. Install yotta

    Install using the yotta windows installer.
    
    http://docs.yottabuild.org/#installing-on-windows

## Making code changes
1. Open this folder in Visual Studio Code.

2. Most interesting file is `./source/Main.cpp`

## Building the .hex file
1. Open this folder in the yotta workspace: Double-click the "Run Yotta" desktop shortcut. This will launch a yotta command prompt. Once there, change directory to this folder.

2. Run the command `yt build`.
3. Find the resultant hex file at `./build/source/kodu-microbit-combined.hex`

## Testing the .hex file from a serial terminal

Install an RS232 terminal app such as [Termite](https://www.compuphase.com/software_termite.htm).

Copy the hex file to the microbit (drag/drop it onto the MICROBIT drive).

If it boots up successfully, it will scroll "Kodu" across the microbit display.

Connect your terminal app to the microbit. Find the COM port using Device Manager. Look under `Ports (COM & LPT)`. Connection configuration:
- Baud Rate: 115200
- Data bits: 8
- Parity: None
- Stop bits: 1

Ping the microbit by sending `P|`. This should result in the microbit sending back a ping reply, and five seconds of sampled sensor data (accelerometer - others can be added as desired - e.g., compass).

Ensure the feature you're developing is testable via serial terminal. Add serial test commands to TESTS.md.

See TESTS.md for more info.

## Testing the .hex file from within Kodu

Copy the hex file to the microbit (drag/drop it onto the MICROBIT drive).

If it boots up successfully, it will scroll "Kodu" across the microbit display.

Also copy the .hex file to `Boku/Content/Microbit`. This ensures that if Kodu feels the need to flash the microbit, it will flash the correct version. Also this is the file you'll want to checkin if all looks good and you're done making changes.

Create a level and write some microbit kode to exercize your changes.

See TESTS.md for more info.

# Testing the Microbit

## Microbit test commands
* Send these commands over a serial port connection using an RS-232 terminal program like [Termite](https://www.compuphase.com/software_termite.htm).
* Whenver the microbit receives a command over serial, it will send back device telemetry for 5 seconds.

#### CMD_PING
Sends a ping, expects a ping reply.

P|

#### CMD_PRINT_TEXT
Print the characters to the display, one at a time.

D|0200|FF|05hello|

#### CMD_SCROLL_TEXT
Scroll the text across the display.

C|0080|FF|05hello|

#### CMD_SET_PIXEL
Sets the upper-left pixel.

I|00|00|FF|

#### CMD_PRINT_DISPLAY_FRAMES

Displays images: left/top edges, right/bottom edges, plus sign, cross sign.

J|04|03E8|FF|VGGGG|03E8|FF|1111V|03E8|FF|44V44|03E8|FF|HA4AH|

## Test Microbit flashing

* If you rebuilt the .hex file: In a file explorer, copy the `kodu-microbit-combined.hex` file to the `Boku\Content\Microbit` folder. This ensures the microbit will be flashed with the latest hex file.
* In a file explorer, drag and drop the file `microbit-smile.hex` to the MICROBIT drive letter. This file is located in the `Boku\Content\Microbit` folder. This will replace the existing `kodu-microbit-combined.hex` image on the microbit if it has been flashed previously.
* Run Kodu. Create a world with microbit tiles. Suggested programming:
    
    On a Puck:
    * WHEN [microbit][tilt] DO [move]
    * WHEN [microbit][A button] DO [microbit say: "Hello!"]
    * WHEN [microbit][B button] DO [microbit scroll images][image 1][image 2]...

* Enter play mode. After a few seconds Kodu should flash the microbit. If successful, "Kodu" will scroll across the microbit display and you should be able to control your character by tilting the microbit different directions.
* Press the buttons to test text and image display functionality. Image scrolling functionality is notoriously unreliable due to the size of the messages being sent over the serial connection. See suggested replacement functionality in FUTURE.md.

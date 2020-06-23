# Proposed v.Next Microbit Functionality
The first round of microbit functionality was implemented *before the microbit existed*. The majority of development and testing was done on a homebrew prototype board, with no software support. Code was written directly against the chipsets. This severely limited what was possible at the time. Today we have at our disposal a mature microbit Device Abstraction Layer (DAL). This is software that provides a complete interface to all device systems. I propose we leverage the DAL's functionlity to improve Kodu's microbit support. This essentially means deprecating some low-level tiles and introducing higher level ones that better represent what the microbit can do. At the same time, I'd like to change some of the existing functionality to account for the unreliable nature of the serial port connection, and to improve design consistency with the rest of the language.

Perusing shared Kodu worlds, I was amazed to see there are *hundreds of worlds* with "microbit" or "micro:bit" in their title or description. Let's give this audience more to work with!

## Proposed Changes:

### DEPRECATE "PWM OUT" ACTUATORS

The low-level "PWM out" actuators can now be deprecated and replaced with new ones that encapsulate the kinds of higher-level things you can do with PWM signals, such as:
* Set the angle of a servo motor.
* Blink an LED.
* Generate an audio tone or play a melody.

These are outlined below.

### ADD "SET SERVO ANGLE" ACTUATOR
If a servo motor is attached to a pin, sets the servo's angle.

Example

`WHEN (some condition) - DO [microbit: set servo angle] [microbit pin] (number: 0 - 180 degrees)`

Default pin: P0

Default angle: 0

Thoughts: An angle picker dialog for this would be amazing.

### ADD "BLINK PIN" ACTUATOR
If an LED is connected to a pin, it will blink at the given frequency.

Example

`WHEN (some condition) - DO [microbit: blink pin] [microbit pin] [frequency] (optional: [duration])`

Default pin: P0

Default frequency: 1Hz.

It's interesting to think about how to stop blinking a pin when keyed to a button press or other toggle event. It might have to happen on a "not" rule, setting the blink frequency to 0.

`WHEN [keyboard] [A] - DO [microbit: blink pin] [P0]`

`WHEN [keyboard] [A] [not] - DO [microbit: blink pin] [P0] [0]`

### ADD "PLAY MELODY" ACTUATOR
If a PC speaker is connected to a pin, play a predefined melody on it. Melodies are defined in the microbit hex file, so we only need to send the id over.

Example

`WHEN (some condition) - DO [microbit: play melody] [microbit pin] [melody]`

Default pin: P0

Default melody: Something fun: jump up or power up, or pick a random one?

Available melody modifiers (we can choose subset):
* dadadum
* entertainer
* prelude
* ode
* nyan
* ringtone
* funk
* blues
* birthday
* wedding
* funeral
* punchline
* baddy
* chase
* ba ding
* wawawawaa
* jump up
* jump down
* power up
* power down

### ADD "MICROBIT GESTURE" INPUT FILTER
Add filters for the different gestures that can be sensed by the microbit. The DAL detects these gestures natively, so we get this "for free".

Gesture filters:
* Tilt Up
* Tilt Down
* Tilt Left
* Tilt Right
* Face Up
* Face Down
* Shake

Example

`WHEN [microbit] [gesture] [shake] - DO [jump]`

### DEPRECATE "MICROBIT SHAKE" SENSOR
Roll this functionality into the Gesture input filter.

### DEPRECATE INDIVIDUAL "SET PIN" ACTUATORS
Replace with a single actuator that takes a [microbit pin] modifier, consistent with the new actuators above.

Example

`WHEN (some condition) - DO [microbit: set pin] [microbit pin] (value: number in [0..255]), or ([on]|[off])`

Default pin: P0

Default value: On

If value is numeric, the pin will utilize analog out.
If value is the [on] or [off] tile, the pin wil utilize digital out.

### DEPRECATE INDIVIDUAL "READ PIN" SENSORS
Don't surface these as sensors. Replace with filter tiles to set a score value from the pin value. Or possibly express as tiles that could be used anywhere numbers are used today. **NEEDS DISCUSSION**.

### DEPRECATE "SHOW IMAGES" ACTUATOR
Showing a series of images on the microbit requires a larger than is comfortable packet sent over the serial connection. Often this packet is corrupt in some way causing the series of images to be clipped. Instead lets just show a single predefined image. See the "show icon" actuator below.

### ADD "SHOW ICON" ACTUATOR
Show a predefined icon on the microbit display, with optional duration. Icons are defined in the microbit hex file, so we only need to send the id.

Example

`WHEN (some condition) - DO [microbit: show icon] [icon] (optional: [duration])`

Possible predefined icons (we can choose a subset):
* Heart
* Small Heart
* Happy
* Sad
* Confused
* Angry
* Asleep
* Surprised
* Silly
* Fabulous
* Meh
* Yes
* No
* Checkerboard
* Diamond
* Small Diamond
* Square
* Small Square
* Dot
* Scissors
* Up Arrow
* Up/Right Arrow
* Right Arrow
* Down/Right Arrow
* Down Arrow
* Down/Left Arrow
* Left Arrow
* Up/Left Arrow
* Block
* Blank

### ADD "SET LED" ACTUATOR
Turns an individual display LED on the display on or off.

Example

`WHEN (some condition) - DO [microbit: set LED] ([on]|[off]) [x] (coord in [0..4]) [y] (coord in [0..4])`

Default value: `[on]`

Default x coord: 0 (or call this `[column]`?)

Default y coord: 0 (or call this `[row]`?)

We actually have 0..255 levels of brightness if we want to expose them. Seems out of scope given Kodu's limited ability to work with or express numbers.

### COMPASS
I experimented with including compass heading in the telemetry sent from the microbit to Kodu, but ran into a problem. Every time the microbit starts up, it enters a compass calibration mode that requires user input before anything else can happen. I haven't found a way around it but will keep looking. It would be great to be able to include compass.

### DOCUMENTATION
**TODO**: Consider how to document the Kodu microbit tiles. Many should be accompanied by simple circuit diagrams or short YouTube videos. Can we reference or borrow from MakeCode's docs?

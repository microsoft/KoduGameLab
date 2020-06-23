#include "MicroBit.h"
#include "MicroBitCompat.h"
#include "Message.h"

//============================================================================

#define KODU_MICROBIT_VERSION 4

#define SAMPLED_STATE_HZ 10
#define SAMPLED_STATE_SECS 5
#define SAMPLED_STATE_ITERATIONS (SAMPLED_STATE_HZ * SAMPLED_STATE_SECS)

#define INIT_CHECKED_STATE() bool ReadOk = true
#define CHECKED_READ(cond)        \
    if (ReadOk && !(cond)) {      \
        errmsg("ERR_PARSE", msg); \
        ReadOk = false;           \
    }
#define CHECKED_ACTION(action) \
    if (ReadOk) {              \
        (action);              \
    }
#define READ_OK() ReadOk

#define PIN_COUNT 21

//============================================================================

static MicroBit s_ubit;
static volatile int s_sendStateIterations;
static volatile bool s_displayBusy;
static volatile int s_buttonState[2] = {MICROBIT_BUTTON_EVT_UP, MICROBIT_BUTTON_EVT_UP};
static volatile bool s_pinsBusy[PIN_COUNT];
static Message s_displayOpMsg;

//============================================================================
// Protocol

enum EProtocol {
    //------------------------------------------------------------------------
    // COMMANDS - Sent from Kodu

    // P
    CMD_PING = 'P',
    // S
    CMD_START = 'S',
    // A<delayMs:word><brightness:byte><count:byte><img:Image>[<img:Image>[...]]
    CMD_SCROLL_IMAGES = 'A',
    // B<durationMs:word><brightness:byte><count:byte><img:Image><img:Image>[...]]
    CMD_PRINT_IMAGES = 'B',
    // C<delayMs:word><brightness:byte><str:String>
    CMD_SCROLL_TEXT = 'C',
    // D<durationMs:word><brightness:byte><str:String>
    CMD_PRINT_TEXT = 'D',
    // E<pin:byte><pinMode:byte>[<pullMode:byte>]
    CMD_CONFIG_INPUT_PIN = 'E',
    // F<pin:byte><mode:byte><value:word>
    CMD_SET_PIN_VALUE = 'F',
    // G<pin:byte><value:word>
    CMD_SET_PIN_SERVO_VALUE = 'G',
    // H<pin:byte><durationMs:word><count:byte><frequency:word>[<frequency:word>...]]
    CMD_PLAY_TONES = 'H',
    // I<x:byte><y:byte><brightness:byte>
    CMD_SET_PIXEL = 'I',
    // J<count:byte>[<durationMs:word><brightness:byte><packedImage:char[5]>...
    CMD_PRINT_DISPLAY_FRAMES = 'J',
    // K<pin:byte><frequencyHz:word><frequencyMultiplier:word><dutyCycle:word>
    CMD_SET_PIN_PWM_OUT = 'K',

    //------------------------------------------------------------------------
    // EVENTS - Sent to Kodu

    // m<str:chars>
    EVT_SYSMSG = 'm',
    // p<version:byte>
    EVT_PING_REPLY = 'p',
    // a<button:byte><state:byte>
    EVT_BUTTON_STATE = 'a',
    // b<gesture:byte>
    EVT_ACCEL_GESTURE = 'b',
    // ca<accX:word><accY:word><accZ:word><pitch:word><roll:word>c<heading:word>p<count:byte><state:PinState>[<state:PinState>...]
    EVT_SAMPLED_STATE = 'c',
};

//============================================================================

//----------------------------------------------------------------------------
void sysmsg(const char* str) {
    Message msg(40);
    msg.writeChar(EVT_SYSMSG);
    msg.writeString(str, true);
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void sysmsg(const char* chars, int count) {
    Message msg(40);
    msg.writeChar(EVT_SYSMSG);
    msg.writeChars(chars, count, true);
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void errmsg(const char* err, Message& badmsg) {
    Message msg(40);
    msg.writeChar(EVT_SYSMSG);
    msg.writeString(err);
    msg.writeChars(badmsg.charBuffer(), badmsg.length(), true);
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void onDisplayFree() {
    s_displayBusy = false;
}

//----------------------------------------------------------------------------
void onPinFree(int pin) {
    if (pin < PIN_COUNT) {
        s_pinsBusy[pin] = false;
    }
}

//----------------------------------------------------------------------------
void onPing() {
    // Send a ping in reply including our version number.
    Message msg(20);
    msg.writeChar(EVT_PING_REPLY);
    msg.writeU8Hex(KODU_MICROBIT_VERSION);
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void onStart() {
    // Reset to initial state.
    s_ubit.io.pin[0].setDigitalValue(0);
    s_ubit.io.pin[0].setDigitalValue(1);
    s_ubit.io.pin[0].setDigitalValue(2);
    s_ubit.display.setBrightness(255);
    s_ubit.display.image.clear();
    s_displayBusy = false;
    for (int i = 0; i < PIN_COUNT; ++i) s_pinsBusy[i] = false;
    s_buttonState[0] = MICROBIT_BUTTON_EVT_UP;
    s_buttonState[1] = MICROBIT_BUTTON_EVT_UP;
    // Send a ping in reply including our version number.
    Message msg(20);
    msg.writeChar(EVT_PING_REPLY);
    msg.writeU8Hex(KODU_MICROBIT_VERSION);
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void scrollImagesFiber() {
    INIT_CHECKED_STATE();
    Message& msg = s_displayOpMsg;
    uint16_t delayMs;
    uint8_t brightness;
    uint8_t imageCount;
    CHECKED_READ(msg.consume(CMD_SCROLL_IMAGES));
    CHECKED_READ(msg.readU16Hex(delayMs));
    CHECKED_READ(msg.readU8Hex(brightness));
    CHECKED_READ(msg.readU8Hex(imageCount));
    if (READ_OK()) {
        s_ubit.display.setBrightness(brightness);
        s_ubit.display.image.clear();
        MicroBitImage image;
        while (READ_OK() && imageCount--) {
            CHECKED_READ(msg.readImage(image));
            CHECKED_ACTION(s_ubit.display.scroll(image, delayMs));
        }
    }
    onDisplayFree();
    release_fiber();
}

//----------------------------------------------------------------------------
void printImagesFiber() {
    INIT_CHECKED_STATE();
    Message& msg = s_displayOpMsg;
    uint16_t durationMs;
    uint8_t brightness;
    uint8_t count;
    CHECKED_READ(msg.consume(CMD_PRINT_IMAGES));
    CHECKED_READ(msg.readU16Hex(durationMs));
    CHECKED_READ(msg.readU8Hex(brightness));
    CHECKED_READ(msg.readU8Hex(count));
    if (READ_OK()) {
        s_ubit.display.setBrightness(brightness);
        s_ubit.display.image.clear();
        MicroBitImage image;
        while (READ_OK() && count-- > 0) {
            CHECKED_READ(msg.readImage(image));
            CHECKED_ACTION(s_ubit.display.print(image, 0, 0, 0, durationMs));
        }
    }
    onDisplayFree();
    release_fiber();
}

//----------------------------------------------------------------------------
void printDisplayFramesFiber() {
    INIT_CHECKED_STATE();
    Message& msg = s_displayOpMsg;
    uint8_t count;
    CHECKED_READ(msg.consume(CMD_PRINT_DISPLAY_FRAMES));
    CHECKED_READ(msg.readU8Hex(count));
    CHECKED_ACTION(s_ubit.display.image.clear());
    while (READ_OK() && count-- > 0) {
        uint16_t durationMs;
        uint8_t brightness;
        MicroBitImage image;
        CHECKED_READ(msg.readU16Hex(durationMs));
        CHECKED_READ(msg.readU8Hex(brightness));
        CHECKED_READ(msg.readImage(image));
        if (READ_OK()) {
            s_ubit.display.setBrightness(brightness);
            s_ubit.display.print(image, 0, 0, 0, durationMs);
            if (durationMs > 0) {
                CHECKED_ACTION(s_ubit.display.image.clear());
            }
        }
    }
    onDisplayFree();
    release_fiber();
}

//----------------------------------------------------------------------------
void scrollTextFiber() {
    INIT_CHECKED_STATE();
    Message& msg = s_displayOpMsg;
    uint16_t delayMs;
    uint8_t brightness;
    ManagedString str;
    CHECKED_READ(msg.consume(CMD_SCROLL_TEXT));
    CHECKED_READ(msg.readU16Hex(delayMs));
    CHECKED_READ(msg.readU8Hex(brightness));
    CHECKED_READ(msg.readString(str));
    CHECKED_ACTION(s_ubit.display.image.clear());
    if (READ_OK() && str.length()) {
        s_ubit.display.setBrightness(brightness);
        s_ubit.display.scroll(str, delayMs);
    }
    onDisplayFree();
    release_fiber();
}

//----------------------------------------------------------------------------
void printTextFiber() {
    INIT_CHECKED_STATE();
    Message& msg = s_displayOpMsg;
    uint16_t durationMs;
    uint8_t brightness;
    ManagedString str;
    CHECKED_READ(msg.consume(CMD_PRINT_TEXT));
    CHECKED_READ(msg.readU16Hex(durationMs));
    CHECKED_READ(msg.readU8Hex(brightness));
    CHECKED_READ(msg.readString(str));
    CHECKED_ACTION(s_ubit.display.image.clear());
    if (READ_OK() && str.length()) {
        s_ubit.display.setBrightness(brightness);
        s_ubit.display.print(str, durationMs);
    }
    onDisplayFree();
    release_fiber();
}

//----------------------------------------------------------------------------
void onScrollImages(Message& msg) {
    if (s_displayBusy) {
        sysmsg("ERR_DISPLAY_BUSY");
        return;
    }
    s_displayBusy = true;
    s_displayOpMsg.copyFrom(msg);
    create_fiber(scrollImagesFiber);
}

//----------------------------------------------------------------------------
void onPrintImages(Message& msg) {
    if (s_displayBusy) {
        sysmsg("ERR_DISPLAY_BUSY");
        return;
    }
    s_displayBusy = true;
    s_displayOpMsg.copyFrom(msg);
    create_fiber(printImagesFiber);
}

//----------------------------------------------------------------------------
void onPrintDisplayFrames(Message& msg) {
    if (s_displayBusy) {
        sysmsg("ERR_DISPLAY_BUSY");
        return;
    }
    s_displayBusy = true;
    s_displayOpMsg.copyFrom(msg);
    create_fiber(printDisplayFramesFiber);
}

//----------------------------------------------------------------------------
void onScrollText(Message& msg) {
    if (s_displayBusy) {
        sysmsg("ERR_DISPLAY_BUSY");
        return;
    }
    s_displayBusy = true;
    s_displayOpMsg.copyFrom(msg);
    create_fiber(scrollTextFiber);
}

//----------------------------------------------------------------------------
void onPrintText(Message& msg) {
    if (s_displayBusy) {
        sysmsg("ERR_DISPLAY_BUSY");
        return;
    }
    s_displayBusy = true;
    s_displayOpMsg.copyFrom(msg);
    create_fiber(printTextFiber);
}

//----------------------------------------------------------------------------
void onConfigInputPin(Message& msg) {
    INIT_CHECKED_STATE();
    // E|<pin:byte>|<pinMode:byte>[|<pullMode:byte>]
    uint8_t pin;
    uint8_t pinMode;
    CHECKED_READ(msg.consume(CMD_CONFIG_INPUT_PIN));
    CHECKED_READ(msg.readU8Hex(pin));
    CHECKED_READ(msg.readU8Hex(pinMode));
    if (pin > 2) {
        return errmsg("ERR_ARGUMENT:pin>2", msg);
    }
    if (pinMode == IO_STATUS_DIGITAL_IN) {
        uint8_t pullMode;
        CHECKED_READ(msg.readU8Hex(pullMode));
        s_ubit.io.pin[pin].getDigitalValue((PinMode)pullMode);
    } else if (pinMode == IO_STATUS_ANALOG_IN) {
        s_ubit.io.pin[pin].getAnalogValue();
    } else {
        return errmsg("ERR_ARGUMENT:pinMode", msg);
    }
}

//----------------------------------------------------------------------------
void onSetPinValue(Message& msg) {
    INIT_CHECKED_STATE();
    uint8_t pin;
    uint8_t pinMode;
    uint16_t pinValue;
    CHECKED_READ(msg.consume(CMD_SET_PIN_VALUE));
    CHECKED_READ(msg.readU8Hex(pin));
    CHECKED_READ(msg.readU8Hex(pinMode));
    CHECKED_READ(msg.readU16Hex(pinValue));
    if (pin > 2) {
        return errmsg("ERR_ARGUMENT:pin>2", msg);
    }
    if (READ_OK()) {
        if (pinMode == IO_STATUS_DIGITAL_OUT) {
            s_ubit.io.pin[pin].setDigitalValue(pinValue ? 1 : 0);
        } else if (pinMode == IO_STATUS_ANALOG_OUT) {
            if (pinValue > MICROBIT_PIN_MAX_OUTPUT) {
                return errmsg("ERR_ARGUMENT:pinValue>1023", msg);
            }
            s_ubit.io.pin[pin].setAnalogValue(pinValue);
        } else {
            return errmsg("ERR_ARGUMENT:pinMode", msg);
        }
    }
}

//----------------------------------------------------------------------------
void onSetPinServoValue(Message& msg) {
    INIT_CHECKED_STATE();
    uint8_t pin;
    uint16_t pinValue;
    CHECKED_READ(msg.consume(CMD_SET_PIN_SERVO_VALUE));
    CHECKED_READ(msg.readU8Hex(pin));
    CHECKED_READ(msg.readU16Hex(pinValue));
    if (pin > 2) {
        return errmsg("ERR_ARGUMENT:pin>2", msg);
    }
    if (READ_OK()) {
        s_ubit.io.pin[pin].setServoValue(pinValue);
    }
}

//----------------------------------------------------------------------------
void onSetPinPwmOut(Message& msg) {
    INIT_CHECKED_STATE();
    uint8_t pin;
    uint16_t frequencyHz;
    uint16_t frequencyMultiplier;
    uint16_t dutyCycle;
    CHECKED_READ(msg.consume(CMD_SET_PIN_PWM_OUT));
    CHECKED_READ(msg.readU8Hex(pin));
    CHECKED_READ(msg.readU16Hex(frequencyHz));
    CHECKED_READ(msg.readU16Hex(frequencyMultiplier));
    CHECKED_READ(msg.readU16Hex(dutyCycle));
    if (pin > 2) {
        return errmsg("ERR_ARGUMENT:pin>2", msg);
    }
    if (dutyCycle > 1023) {
        return errmsg("ERR_ARGUMENT:dutyCycle>1023", msg);
    }
    if (frequencyHz == 0) {
        return errmsg("ERR_ARGUMENT:frequencyHz==0", msg);
    }
    if (frequencyMultiplier == 0) {
        return errmsg("ERR_ARGUMENT:frequencyMultiplier==0", msg);
    }
    if (READ_OK()) {
        int periodUs = (int)(1000000.0f / (frequencyHz * frequencyMultiplier));
        s_ubit.io.pin[pin].setAnalogValue(dutyCycle);
        s_ubit.io.pin[pin].setAnalogPeriodUs(periodUs);
    }
}

//----------------------------------------------------------------------------
void playTonesFiber(void* param) {
    INIT_CHECKED_STATE();
    Message* pmsg = (Message*)param;
    Message& msg = *pmsg;
    uint8_t pinId;
    CHECKED_READ(msg.consume(CMD_PLAY_TONES));
    CHECKED_READ(msg.readU8Hex(pinId));
    if (READ_OK()) {
        if (pinId > 2) {
            errmsg("ERR_ARGUMENT:pin", msg);
        } else if (s_pinsBusy[pinId]) {
            errmsg("ERR_PIN_BUSY", msg);
        } else {
            s_pinsBusy[pinId] = true;
            uint16_t durationMs;
            uint8_t count;
            CHECKED_READ(msg.readU16Hex(durationMs));
            CHECKED_READ(msg.readU8Hex(count));
            MicroBitPin& pin = s_ubit.io.pin[pinId];
            while (count--) {
                uint16_t frequency;
                CHECKED_READ(msg.readU16Hex(frequency));
                if (!READ_OK())
                    break;
                pin.setAnalogValue(512);
                pin.setAnalogPeriodUs(1000000 / frequency);
                if (durationMs == 0)
                    break;
                fiber_sleep(durationMs);
            }
            if (durationMs) {
                pin.setAnalogValue(0);
            }
            onPinFree(pinId);
        }
    }
    delete pmsg;
    release_fiber();
}

//----------------------------------------------------------------------------
void onPlayTones(Message& msg) {
    INIT_CHECKED_STATE();
    uint8_t pinId = (uint8_t)-1;
    CHECKED_READ(msg.consume(CMD_PLAY_TONES));
    CHECKED_READ(msg.readU8Hex(pinId));
    if (pinId > 2) {
        errmsg("ERR_ARGUMENT:pin", msg);
        return;
    }
    if (s_pinsBusy[pinId]) {
        sysmsg("ERR_PIN_BUSY");
        return;
    }
    create_fiber(playTonesFiber, new Message(msg, true));
}

//----------------------------------------------------------------------------
void onSetPixel(Message& msg) {
    INIT_CHECKED_STATE();
    uint8_t x, y, brightness;
    CHECKED_READ(msg.consume(CMD_SET_PIXEL));
    CHECKED_READ(msg.readU8Hex(x));
    CHECKED_READ(msg.readU8Hex(y));
    CHECKED_READ(msg.readU8Hex(brightness));
    CHECKED_ACTION(s_ubit.display.image.setPixelValue(x, y, brightness));
}

//----------------------------------------------------------------------------
void dispatchMessage(const char* buf, int length) {
    Message msg(buf, length);
    char cmd = 0;
    msg.readChar(cmd);
    msg.rewind();

    switch (cmd) {
        case CMD_PING:
            return onPing();
        case CMD_START:
            return onStart();
        case CMD_SCROLL_IMAGES:
            return onScrollImages(msg);
        case CMD_PRINT_IMAGES:
            return onPrintImages(msg);
        case CMD_SCROLL_TEXT:
            return onScrollText(msg);
        case CMD_PRINT_TEXT:
            return onPrintText(msg);
        case CMD_CONFIG_INPUT_PIN:
            return onConfigInputPin(msg);
        case CMD_SET_PIN_VALUE:
            return onSetPinValue(msg);
        case CMD_SET_PIN_SERVO_VALUE:
            return onSetPinServoValue(msg);
        case CMD_PLAY_TONES:
            return onPlayTones(msg);
        case CMD_SET_PIXEL:
            return onSetPixel(msg);
        case CMD_PRINT_DISPLAY_FRAMES:
            return onPrintDisplayFrames(msg);
        case CMD_SET_PIN_PWM_OUT:
            return onSetPinPwmOut(msg);
        default:
            return errmsg("ERR_UNKNOWN", msg);
    }
}

//----------------------------------------------------------------------------
void onReceiveMessage(MicroBitEvent) {
    s_sendStateIterations = SAMPLED_STATE_ITERATIONS;
    ManagedString msg = s_ubit.serial.readUntil("\n", SYNC_SLEEP);
    dispatchMessage(msg.toCharArray(), msg.length());
    s_ubit.serial.eventOn("\n", ASYNC);
}

//----------------------------------------------------------------------------
void onButton(MicroBitEvent e) {
    if (e.source == 1)
        s_buttonState[0] = e.value;
    if (e.source == 2)
        s_buttonState[1] = e.value;
    Message msg(20);
    msg.writeChar(EVT_BUTTON_STATE);
    msg.writeU8Hex(e.source);
    msg.writeU8Hex(e.value);
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void onAccelGesture(MicroBitEvent e) {
    Message msg(20);
    msg.writeChar(EVT_ACCEL_GESTURE);
    msg.writeU8Hex(e.value);
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void sendSampledState() {
    Message msg(50);
    msg.writeChar(EVT_SAMPLED_STATE);
    // Write button states
    msg.writeChar('b');
    msg.writeU8Hex(s_buttonState[0]);
    msg.writeU8Hex(s_buttonState[1]);
    // Write accelerometer
    msg.writeChar('a');
    msg.writeU16Hex(s_ubit.accelerometer.getX());
    msg.writeU16Hex(s_ubit.accelerometer.getY());
    msg.writeU16Hex(s_ubit.accelerometer.getZ());
    // msg.writeU16Hex(s_ubit.accelerometer.getPitch());
    // msg.writeU16Hex(s_ubit.accelerometer.getRoll());
    // Write compass heading - disabled because of the calibration step that
    // happens every time Kodu enters play mode.
    /*
    msg.writeChar('c');
    int compassHeading = max(0, s_ubit.compass.heading());
    msg.writeU16Hex(compassHeading);
    */
    // Write input pins
    uint8_t pinCount = 0;
    for (int i = 0; i < 3; ++i) {
        if (s_ubit.io.pin[i].isInput()) {
            ++pinCount;
        }
    }
    msg.writeChar('p');
    msg.writeU8Hex(pinCount);
    for (int i = 0; i < 3; ++i) {
        MicroBitPin& pin = s_ubit.io.pin[i];
        if (pin.isInput()) {
            // pin id
            msg.writeU8Hex(i);
            if (pin.isAnalog()) {
                // analog value
                msg.writeChar('a');
                msg.writeU16Hex(pin.getAnalogValue());
            } else {
                // digital value
                msg.writeChar('d');
                msg.writeU16Hex(pin.getDigitalValue());
            }
        }
    }
    s_ubit.serial.send(msg.byteBuffer(), msg.finalize());
}

//----------------------------------------------------------------------------
void sendSampledStateFiber() {
    while (1) {
        if (s_sendStateIterations > 0) {
            s_sendStateIterations -= 1;
            sendSampledState();
        }
        fiber_sleep(1000 / SAMPLED_STATE_HZ);
    }
    release_fiber();
}

//----------------------------------------------------------------------------
int main() {
    // Startup the micro:bit DAL.
    s_ubit.init();

    // Configure the display.
    s_ubit.display.enable();
    s_ubit.display.setDisplayMode(DISPLAY_MODE_GREYSCALE);
    s_ubit.display.setBrightness(255);

    // Scroll "Kodu" while we're getting things setup.
    s_ubit.display.scrollAsync("Kodu", 80);

    // Configure the accelerometer.
    s_ubit.accelerometer.setRange(2);  // 2G
    s_ubit.accelerometer.configure();

    // Configure buttons.
    s_ubit.buttonA.setEventConfiguration(MICROBIT_BUTTON_SIMPLE_EVENTS);
    s_ubit.buttonB.setEventConfiguration(MICROBIT_BUTTON_SIMPLE_EVENTS);

    // Configure serial comms.
    s_ubit.serial.baud(115200);
    s_ubit.serial.setRxBufferSize(128);
    s_ubit.serial.setTxBufferSize(128);
    s_ubit.serial.eventOn("\n", ASYNC);

    // Set up event handlers.
    s_ubit.messageBus.listen(MICROBIT_ID_BUTTON_A, MICROBIT_EVT_ANY, onButton);
    s_ubit.messageBus.listen(MICROBIT_ID_BUTTON_B, MICROBIT_EVT_ANY, onButton);
    s_ubit.messageBus.listen(MICROBIT_ID_GESTURE, MICROBIT_EVT_ANY, onAccelGesture);
    s_ubit.messageBus.listen(MICROBIT_ID_SERIAL, MICROBIT_SERIAL_EVT_DELIM_MATCH,
                             onReceiveMessage);

    // Start the "sampled state" send loop.
    create_fiber(sendSampledStateFiber);

    // Main fiber can exit now.
    release_fiber();
    return 0;
}

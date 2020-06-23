using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

#if !NETFX_CORE
using System.IO.Ports;
#endif

using Boku.Common;

namespace Boku.Input
{
#if !NETFX_CORE
    /// <summary>
    /// Represents the interface to a connected microbit.
    /// </summary>
    public class Microbit : IDisposable
    {
        private const string DriverFilename = "kodu-microbit-combined.hex";
        private const int KoduMicroBitVersion = 4;
        private const int DefaultScrollSpeed = 120;
        private const int DefaultPrintSpeed = 400;
        private const int DefaultBrightness = 0xff;
        private const short MinAccel = -1024;
        private const short MaxAccel = 1024;

        public static class Protocol
        {
            public const char
                //-------------------------------------------------------------
                // COMMANDS - Sent from Kodu to microbit

                // P
                CMD_PING = 'P',
                // S
                CMD_START = 'S',
                // A<delayMs><durationMs:ushort><count:byte><packedImage:char[5]>...
                CMD_SCROLL_IMAGES = 'A',
                // B<durationMs:word><count:byte><img:Image>[<img:Image>[...]]
                CMD_PRINT_IMAGES = 'B',
                // C<brightness:byte><durationMs:word><str:String>
                CMD_SCROLL_TEXT = 'C',
                // D<brightness:byte><delayMs:word><str:String>
                CMD_PRINT_TEXT = 'D',
                // E<pin:byte><pinMode:byte>[<pullMode:byte>]
                CMD_CONFIG_INPUT_PIN = 'E',
                // F<pin:byte><mode:byte><value:word>
                CMD_SET_PIN_VALUE = 'F',
                // G<pin:byte><value:word>
                CMD_SET_PIN_SERVO_VALUE = 'G',
                // H<pin:byte><durationMs:word><count:byte><frequency:word>...
                CMD_PLAY_TONES = 'H',
                // I<x:byte><y:byte><brightness:byte>
                CMD_SET_PIXEL = 'I',
                // J<count:byte>[<durationMs:word><brightness:byte><packedImage:char[5]>...
                CMD_PRINT_DISPLAY_FRAMES = 'J',
                // K<pin:byte><frequencyHz:word><frequencyMultiplier:word><dutyCycle:word>
                CMD_SET_PIN_PWM_OUT = 'K',

                //-------------------------------------------------------------
                // EVENTS - Sent to Kodu from microbit

                // m<str:chars>
                EVT_SYSMSG = 'm',
                // p<version:byte>
                EVT_PING_REPLY = 'p',
                // a<button:byte><state:byte>
                EVT_BUTTON_STATE = 'a',
                // b<gesture:byte>
                EVT_ACCEL_GESTURE = 'b',
                // ca<accX:short><accY:short><accZ:short><pitch:short><roll:short>c<heading:short>p<count:byte><state:PinState>[<state:PinState>...]
                EVT_SAMPLED_STATE = 'c',
                // d
                EVT_DISPLAY_FREE = 'd',
                // e
                EVT_PIN_FREE = 'e';
        }

        public enum EPinDirection
        {
            Disconnected = 0,
            Output = 1,
            Input = 2
        }

        public enum EPinOperatingMode
        {
            None = 0,
            Digital = 1,
            Analog = 2
        }

        public enum EPinDigitalMode
        {
            In = 1,
            Out = 2
        }

        public enum EPinAnalogMode
        {
            In = 4,
            Out = 8
        }

        public enum EPinPullMode
        {
            PullNone = 0,
            PullUp = 1,
            PullDown = 2,
            OpenDrain = 3,
        }

        public enum EButton
        {
            A = 1,
            B = 2
        }

        public enum EButtonState
        {
            Down = 1,
            Up = 2
        }

        public enum EAccelerometerGesture
        {
            None = 0,
            TiltUp = 1,
            TiltDown = 2,
            TiltLeft = 3,
            TiltRight = 4,
            FaceUp = 5,
            FaceDown = 6,
            Freefall = 7,
            ThreeG = 8,
            SixG = 9,
            EightG = 10,
            Shake = 11
        }

        public struct PinState
        {
            public int Id;
            public int Value;
            public EPinDirection Direction;
            public EPinOperatingMode Mode;
            public bool Busy;
            public int FrequencyHz;
            public int FrequencyMultiplier;
            public int DutyCycle;

            public PinState(int id)
            {
                Id = id;
                Value = 0;
                Direction = EPinDirection.Disconnected;
                Mode = EPinOperatingMode.None;
                Busy = false;
                FrequencyHz = 20000;
                FrequencyMultiplier = 1;
                DutyCycle = 511;
            }

            public void Clear()
            {
                Value = 0;
                Direction = EPinDirection.Disconnected;
                Mode = EPinOperatingMode.None;
                Busy = false;
            }
        }

        public struct ButtonState
        {
            private EButton _id;
            public EButtonState State;

            public int Id { get { return (int)_id; } }

            public ButtonState(EButton id)
            {
                _id = id;
                State = EButtonState.Up;
            }

            public void Clear()
            {
                State = EButtonState.Up;
            }

            public bool IsPressed()
            {
                return State == EButtonState.Down;
            }
        }

        public struct DeviceState
        {
            public int Generation;
            public ButtonState[] Buttons;
            public Vector3 Acc;
            public int AccPitch;
            public int AccRoll;
            public EAccelerometerGesture AccGesture;
            public int Heading;
            public PinState[] Pins;
            public ButtonState ButtonA { get { return Buttons[0]; } }
            public ButtonState ButtonB { get { return Buttons[1]; } }

            public DeviceState(int _)
            {
                Generation = 0;
                Buttons = new ButtonState[2];
                Buttons[0] = new ButtonState(EButton.A);
                Buttons[1] = new ButtonState(EButton.B);
                Acc = Vector3.Zero;
                AccPitch = 0;
                AccRoll = 0;
                AccGesture = EAccelerometerGesture.None;
                Heading = 0;
                Pins = new PinState[3];
                Pins[0] = new PinState(0);
                Pins[1] = new PinState(1);
                Pins[2] = new PinState(2);
            }

            public void Clear()
            {
                Generation = 0;
                Buttons[0].Clear();
                Buttons[1].Clear();
                Acc = Vector3.Zero;
                AccPitch = 0;
                AccRoll = 0;
                AccGesture = EAccelerometerGesture.None;
                Heading = 0;
                Pins[0].Clear();
                Pins[1].Clear();
                Pins[2].Clear();
            }
        }

        private CommPort _port;
        private string _drive;
        private DeviceState _curr = new DeviceState(0);
        private DeviceState _prev = new DeviceState(0);
        private DateTime _lastMsgSendTime;
        private DateTime _lastMsgRecvTime;
        private DateTime _creationTime;
        private MicrobitDesc _desc;
        private EDeviceStatus _status;
        private EFlashStatus _flashStatus;
        private int _version;
        private DateTime _displayFreeTime = DateTime.Now;

        public enum EDeviceStatus
        {
            CREATED,
            FLASHING,
            FLASHED,
            READY,
            ERROR,
        }

        public enum EFlashStatus
        {
            NOT_FLASHED,
            FLASH_IN_PROGRESS,
            FLASH_SUCCESSFUL,
            FLASH_FAILED
        }

        public string Drive { get { return _drive; } }
        public bool IsConnected { get { return _port.IsOpen; } }
        public DeviceState State { get { return _curr; } }
        public EDeviceStatus Status { get { return _status; } }
        public bool DisplayBusy { get { return _displayFreeTime > DateTime.Now; } }

        /// <summary>
        /// Creates an interface to a microbit.
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static Microbit Create(MicrobitDesc desc)
        {
            Microbit microbit = null;
            try
            {
                microbit = new Microbit(desc);

                // Microbit is provisionally ready. May need to flash.
                return microbit;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to create microbit: " + ex.Message);

                // Cleanup the microbit.
                if (microbit != null)
                {
                    microbit.Dispose();
                }
            }
            // Failed to produce a microbit.
            return null;
        }

        private void OnEvtPingReply(MicroBitMessageReader reader)
        {
            while (true)
            {
                if (!reader.ReadU8Hex(out _version)) break;
                if (_version == KoduMicroBitVersion)
                {
                    _status = EDeviceStatus.READY;
                }
                break;
            }
        }

        private void OnEvtSysMsg(MicroBitMessageReader reader)
        {
            while (true)
            {
                string msg;
                if (!reader.ReadToEnd(out msg)) break;
                Console.WriteLine("MICROBIT_SYSMSG: " + msg);
                break;
            }
        }

        private void OnEvtAccelGesture(MicroBitMessageReader reader)
        {
            while (true)
            {
                int gesture;
                if (!reader.ReadU8Hex(out gesture)) break;
                _curr.AccGesture = (EAccelerometerGesture)gesture;
                _curr.Generation += 1;
                break;
            }
        }

        private void OnEvtButtonState(MicroBitMessageReader reader)
        {
            while (true)
            {
                int button;
                int state;
                if (!reader.ReadU8Hex(out button)) break;
                if (!reader.ReadU8Hex(out state)) break;
                if (button == (int)EButton.A)
                    _curr.Buttons[0].State = (EButtonState)state;
                if (button == (int)EButton.B)
                    _curr.Buttons[1].State = (EButtonState)state;
                _curr.Generation += 1;
                break;
            }
        }

        private void OnEvtDisplayFree(MicroBitMessageReader reader)
        {
            // _curr.DisplayBusy = false;
        }

        private void OnEvtPinFree(MicroBitMessageReader reader)
        {
            /*
            while (true)
            {
                int pin;
                if (!reader.ReadU8Hex(out pin)) break;
                if (pin >= 0 && pin <= 2)
                    _curr.Pins[pin].Busy = false;
                break;
            }
             * */
        }

        private void OnEvtSampledState(MicroBitMessageReader reader)
        {
            // Copy the current state to the previous state.
            _prev = _curr;
            // Read the new current state.
            while (true)
            {
                short accX, accY, accZ;
                // int accPitch, accRoll;
                // int heading;
                int pinCount;
                int buttonA; int buttonB;
                PinState[] pins = new PinState[3];
                // Read button states
                if (!reader.Consume('b')) break;
                if (!reader.ReadU8Hex(out buttonA)) break;
                if (!reader.ReadU8Hex(out buttonB)) break;
                // Read accelerometer
                if (!reader.Consume('a')) break;
                if (!reader.ReadSignedU16Hex(out accX)) break;
                if (!reader.ReadSignedU16Hex(out accY)) break;
                if (!reader.ReadSignedU16Hex(out accZ)) break;
                // if (!reader.ReadU16Hex(out accPitch)) break;
                // if (!reader.ReadU16Hex(out accRoll)) break;
                // Read compass
                // if (!reader.Consume('c')) break;
                // if (!reader.ReadU16Hex(out heading)) break;
                // Read input pins
                if (!reader.Consume('p')) break;
                if (!reader.ReadU8Hex(out pinCount)) break;
                int pinsRemaining = pinCount;
                // Make sure we're in range.
                if (pinsRemaining > 3) break;
                while (pinsRemaining > 0)
                {
                    int pinId, pinValue;
                    char pinMode;
                    if (!reader.ReadU8Hex(out pinId)) break;
                    if (pinId > 2) break;
                    if (!reader.ReadChar(out pinMode)) break;
                    if (pinMode != 'a' && pinMode != 'd') break;
                    if (!reader.ReadU16Hex(out pinValue)) break;
                    pins[pinId] = new PinState(pinId);
                    pins[pinId].Direction = EPinDirection.Input;
                    pins[pinId].Mode = pinMode == 'a' ? EPinOperatingMode.Analog : EPinOperatingMode.Digital;
                    pins[pinId].Value = pinValue;
                    pinsRemaining--;
                }
                // If we didn't read all the pins then something is wrong with the message.
                if (pinsRemaining > 0) break;
                // Apply button states
                _curr.Buttons[0].State = (EButtonState)buttonA;
                _curr.Buttons[1].State = (EButtonState)buttonB;
                // Apply accelerometer
                accX = MyMath.Clamp(accX, MinAccel, MaxAccel);
                accY = MyMath.Clamp(accY, MinAccel, MaxAccel);
                accZ = MyMath.Clamp(accZ, MinAccel, MaxAccel);
                Vector3 acc = new Vector3(accY, accX, accZ);
                acc.Normalize();
                _curr.Acc = MyMath.NanProtect(acc, Vector3.Zero);
                // _curr.AccPitch = accPitch;
                // _curr.AccRoll = accRoll;
                // Apply compass
                // _curr.Heading = heading;
                // Apply pins
                for (int i = 0; i < pinCount; ++i)
                {
                    _curr.Pins[pins[i].Id] = pins[i];
                }
                // Inc generation
                _curr.Generation += 1;
                break;
            }
        }

        private void OnCommOpen()
        {
            _lastMsgRecvTime = DateTime.Now;
            _curr.Clear();
            _prev.Clear();
            Start();
        }

        private void OnCommReceive(string command)
        {
            // System.Diagnostics.Debug.WriteLine("Recv: " + command);

            // If there's no data, continue.
            if (command.Length == 0) return;

            MicroBitMessageReader reader = new MicroBitMessageReader(command);

            // If there's no data, continue.
            char evt;
            if (!reader.ReadChar(out evt))
            {
                return;
            }

            _lastMsgRecvTime = DateTime.Now;

            // Process ping reply messages.
            switch (evt)
            {
                case Protocol.EVT_PING_REPLY:
                    OnEvtPingReply(reader);
                    break;
            }

            // Don't read messages other than ping replies until device is ready.
            if (_status != EDeviceStatus.READY)
            {
                return;
            }

            // Device is ready. Process remaining message types.
            switch (evt)
            {
                case Protocol.EVT_SYSMSG:
                    OnEvtSysMsg(reader);
                    break;

                case Protocol.EVT_ACCEL_GESTURE:
                    OnEvtAccelGesture(reader);
                    break;

                case Protocol.EVT_BUTTON_STATE:
                    OnEvtButtonState(reader);
                    break;

                case Protocol.EVT_DISPLAY_FREE:
                    OnEvtDisplayFree(reader);
                    break;

                case Protocol.EVT_PIN_FREE:
                    OnEvtPinFree(reader);
                    break;

                case Protocol.EVT_SAMPLED_STATE:
                    OnEvtSampledState(reader);
                    break;
            }
        }

        private Microbit(MicrobitDesc desc)
        {
            _creationTime = DateTime.Now;
            _desc = desc;
            _port = new CommPort(desc.COM, 115200, OnCommReceive, OnCommOpen);
            _drive = _desc.Drive;
        }

        public void Dispose()
        {
            _port.Dispose();
        }

        private bool ShouldFlash()
        {
            if (_status == EDeviceStatus.CREATED &&
                _flashStatus == EFlashStatus.NOT_FLASHED &&
                (DateTime.Now - _creationTime > TimeSpan.FromSeconds(4.0)))
            {
                return true;
            }
            return false;
        }

        private void StartFlashing()
        {
            if (_status != EDeviceStatus.FLASHING)
            {
                _status = EDeviceStatus.FLASHING;
                Thread t = new Thread(new ThreadStart(FlashIt));
                t.Start();
            }
        }

        private void FlashIt()
        {
            Flash();
        }

        private bool Flash()
        {
            try
            {
                _flashStatus = EFlashStatus.FLASH_IN_PROGRESS;
                string filename = Path.Combine(Storage4.TitleLocation, @"Content", @"Microbit", DriverFilename);
                File.Copy(filename, Path.Combine(Drive, DriverFilename));
                _status = EDeviceStatus.FLASHED;
                _flashStatus = EFlashStatus.FLASH_SUCCESSFUL;
            }
            catch
            {
                _status = EDeviceStatus.ERROR;
                _flashStatus = EFlashStatus.FLASH_FAILED;
                System.Diagnostics.Debug.WriteLine("Failed to flash microbit.");
                return false;
            }

            return true;
        }

        private void QueueForSend(string cmd)
        {
            // System.Diagnostics.Debug.WriteLine("Send: " + cmd);
            _lastMsgSendTime = DateTime.Now;
            this._port.WriteLine(cmd);
        }

        private void Ping()
        {
            // Send a Ping even if the device isn't READY. If we get a response, and
            // if version numbers match, we'll update the device to the READY state.
            if (!_port.IsOpen) return;
            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_PING);
            QueueForSend(writer.ToString());
        }

        private void Start()
        {
            // A Start command is like a Ping, but also causes the microbit to reset
            // to the initial state (display blank, pins off).
            // Send a Start even if the device isn't READY. If we get a response, and
            // if version numbers match, we'll update the device to the READY state.
            if (!_port.IsOpen) return;
            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_START);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Set the pin's output value. Will configure the pin for output if necessary.
        /// </summary>
        /// <param name="pin">The id of the pin.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="operatingMode">The operating mode of the pin.</param>
        public void SetPinValue(int pin, int value, EPinOperatingMode operatingMode)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (pin < 0 || pin > 2) return;
            if (value < 0 || value > 0xff) return;
            if (_curr.Pins[pin].Busy) return;

            // Don't send the same value over and over.
            if (_curr.Pins[pin].Mode == operatingMode && _curr.Pins[pin].Value == value) return;

            _curr.Pins[pin].Mode = operatingMode;
            _curr.Pins[pin].Value = value;

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_SET_PIN_VALUE);
            writer.WriteU8Hex(pin);
            writer.WriteU8Hex(operatingMode == EPinOperatingMode.Digital ? (int)EPinDigitalMode.Out : (int)EPinAnalogMode.Out);
            writer.WriteU16Hex(value);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// If a servo motor is attached, set its angle.
        /// </summary>
        /// <param name="pin">The id of the pin.</param>
        /// <param name="angle">The angle to set in [0..180].</param>
        public void SetPinServoAngle(int pin, int angle)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (pin < 0 || pin > 2) return;
            if (angle < 0 || angle > 180) return;
            if (_curr.Pins[pin].Busy) return;

            // Don't send the same value over and over.
            if (_curr.Pins[pin].Mode == EPinOperatingMode.Analog && _curr.Pins[pin].Value == angle) return;

            _curr.Pins[pin].Mode = EPinOperatingMode.Analog;
            _curr.Pins[pin].Value = angle;

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_SET_PIN_SERVO_VALUE);
            writer.WriteU8Hex(pin);
            writer.WriteU8Hex(angle);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Set the pin's PWM pulse frequency, producing a pulse-width-modulated (PWM) signal. (Frequency only).
        /// Will configure the pin for analog output if necessary.
        /// </summary>
        /// <param name="pin">The id of the pin.</param>
        /// <param name="frequencyHz">The frequency in Hertz.</param>
        /// <param name="multiplier">The frequency multiplier. Value of 1000 would convert the frequency to KHz, for example.</param>
        public void SetPinPwmFrequency(int pin, int frequencyHz, int multiplier)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (pin < 0 || pin > 2) return;
            if (_curr.Pins[pin].Busy) return;
            if (_curr.Pins[pin].Mode == EPinOperatingMode.Analog &&
                _curr.Pins[pin].FrequencyHz == frequencyHz &&
                _curr.Pins[pin].FrequencyMultiplier == multiplier) return;

            _curr.Pins[pin].Mode = EPinOperatingMode.Analog;
            _curr.Pins[pin].FrequencyHz = frequencyHz;
            _curr.Pins[pin].FrequencyMultiplier = multiplier;

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_SET_PIN_PWM_OUT);
            writer.WriteU8Hex(pin);
            writer.WriteU16Hex(_curr.Pins[pin].FrequencyHz);
            writer.WriteU16Hex(_curr.Pins[pin].FrequencyMultiplier);
            writer.WriteU16Hex(_curr.Pins[pin].DutyCycle);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Set the pin's PWM pulse width, producing a pulse-width-modulated (PWM) signal. (Duty cycle only).
        /// Will configure the pin for analog output if necessary.
        /// </summary>
        /// <param name="pin">The id of the pin.</param>
        /// <param name="dutyCyclePct">Percent of the cycle in which the signal is "on", in [0..1] range, where 0 is never on, and 1 is always on. A value of 0.5 would be 50% on.</param>
        public void SetPinPwmDutyCycle(int pin, float dutyCyclePct)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (pin < 0 || pin > 2) return;
            if (_curr.Pins[pin].Busy) return;
            if (dutyCyclePct < 0 || dutyCyclePct > 1) return;

            int dutyCycle = (int)(dutyCyclePct * 1023);
            if (_curr.Pins[pin].Mode == EPinOperatingMode.Analog &&
                _curr.Pins[pin].DutyCycle == dutyCycle) return;

            _curr.Pins[pin].DutyCycle = dutyCycle;

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_SET_PIN_PWM_OUT);
            writer.WriteU8Hex(pin);
            writer.WriteU16Hex(_curr.Pins[pin].FrequencyHz);
            writer.WriteU16Hex(_curr.Pins[pin].FrequencyMultiplier);
            writer.WriteU16Hex(_curr.Pins[pin].DutyCycle);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Read the pin's last known value. Will configure the pin for input if necessary.
        /// </summary>
        /// <param name="pin">The id of the pin.</param>
        /// <param name="operatingMode">The operating mode of the pin.</param>
        /// <param name="pullMode">The pull mode of the pin. Applicable for digital inputs only.</param>
        /// <returns>The most recent sampled pin value.</returns>
        public int ReadPinValue(int pin, EPinOperatingMode operatingMode, EPinPullMode pullMode = EPinPullMode.PullNone)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return 0;
            if (pin < 0 || pin > 2) return 0;

            // If this pin is already configured for input, return the current value.
            if (_curr.Pins[pin].Mode == operatingMode) return _curr.Pins[pin].Value;

            _curr.Pins[pin].Mode = operatingMode;
            _curr.Pins[pin].Value = 0;

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_CONFIG_INPUT_PIN);
            writer.WriteU8Hex(pin);
            if (operatingMode == EPinOperatingMode.Analog)
            {
                writer.WriteU8Hex((int)EPinAnalogMode.In);
            }
            else
            {
                writer.WriteU8Hex((int)EPinDigitalMode.In);
                writer.WriteU8Hex((int)pullMode);
            }
            QueueForSend(writer.ToString());

            return 0;
        }

        /// <summary>
        /// Scrolls a series of images across the microbit screen.
        /// </summary>
        /// <param name="images">The list of images to scroll.</param>
        /// <param name="delayMs">The delay between scroll steps.</param>
        /// <param name="brightness">The brightness of the screen.</param>
        public void ScrollImages(List<MicroBitImage> images, int delayMs = DefaultScrollSpeed, int brightness = DefaultBrightness)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (images.Count() == 0 || images.Count() > 0xff) return;
            if (delayMs < 0 || delayMs > 0xffff) return;
            if (brightness < 0 || brightness > 0xff) return;
            if (DisplayBusy)
            {
                // System.Diagnostics.Debug.WriteLine("Display Busy");
                return;
            }

            // Block the display.
            int durationMs = images.Count * delayMs * 5;
            _displayFreeTime = DateTime.Now + TimeSpan.FromMilliseconds(durationMs);

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_SCROLL_IMAGES);
            writer.WriteU16Hex(delayMs);
            writer.WriteU8Hex(brightness);
            writer.WriteU8Hex(images.Count());
            foreach (MicroBitImage image in images)
            {
                string packed = image.Packed;
                writer.WriteChars(packed);
            }
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Prints a series of images across the microbit screen.
        /// </summary>
        /// <param name="images">The list of images to print.</param>
        /// <param name="durationMs">The duration of time to show each image. 0 = infinite.</param>
        /// <param name="brightness">The brightness of the screen.</param>
        public void PrintImages(List<MicroBitImage> images, int durationMs = DefaultPrintSpeed, int brightness = DefaultBrightness)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (images.Count() == 0 || images.Count() > 0xff) return;
            if (durationMs < 0 || durationMs > 0xffff) return;
            if (brightness < 0 || brightness > 0xff) return;
            if (DisplayBusy)
            {
                // System.Diagnostics.Debug.WriteLine("Display Busy");
                return;
            }

            // Block the display.
            _displayFreeTime = DateTime.Now + TimeSpan.FromMilliseconds(durationMs);

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_PRINT_IMAGES);
            writer.WriteU16Hex(durationMs);
            writer.WriteU8Hex(brightness);
            writer.WriteU8Hex(images.Count());
            foreach (MicroBitImage image in images)
            {
                string packed = image.Packed;
                writer.WriteChars(packed);
            }
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Prints a series of images across the microbit screen, each with its own brightness and duration.
        /// </summary>
        /// <param name="frames">The set of display frames to print.</param>
        public void PrintDisplayFrames(IEnumerable<MicroBitDisplayFrame> frames)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (frames.Count() == 0 || frames.Count() > 0xff) return;
            if (DisplayBusy)
            {
                // System.Diagnostics.Debug.WriteLine("Display Busy");
                return;
            }

            int totalDurationMs = 0;

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_PRINT_DISPLAY_FRAMES);
            writer.WriteU8Hex(frames.Count());
            foreach (MicroBitDisplayFrame frame in frames)
            {
                int durationMs = (int)(frame.Duration * 1000);
                int brightness = frame.Brightness;
                string packed = frame.Image.Packed;
                durationMs = MyMath.Clamp(durationMs, 0, 0xffff);
                brightness = MyMath.Clamp(brightness, 0, 0xff);
                writer.WriteU16Hex(durationMs);
                writer.WriteU8Hex(brightness);
                writer.WriteChars(packed);
                totalDurationMs += durationMs;
            }

            // Block the display
            _displayFreeTime = DateTime.Now + TimeSpan.FromMilliseconds(totalDurationMs);

            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Scrolls a text string across the microbit screen.
        /// </summary>
        /// <param name="str">The string to scroll.</param>
        /// <param name="delayMs">The delay between scroll steps.</param>
        /// <param name="brightness">The brightness of the screen.</param>
        public void ScrollText(string str, int delayMs = DefaultScrollSpeed, int brightness = DefaultBrightness)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (str.Length == 0 || str.Length > 0xff) return;
            if (delayMs < 0 || delayMs > 0xffff) return;
            if (brightness < 0 || brightness > 0xff) return;
            if (DisplayBusy)
            {
                // System.Diagnostics.Debug.WriteLine("Display Busy");
                return;
            }

            // Block the display.
            int durationMs = str.Length * delayMs * 5;
            _displayFreeTime = DateTime.Now + TimeSpan.FromMilliseconds(durationMs);

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_SCROLL_TEXT);
            writer.WriteU16Hex(delayMs);
            writer.WriteU8Hex(brightness);
            writer.WriteString(str);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Prints a text string on the microbit screen, one character at a time.
        /// </summary>
        /// <param name="str">The string to print.</param>
        /// <param name="durationMs">The duration of time to display each character.</param>
        /// <param name="brightness">The brightness of the screen.</param>
        public void PrintText(string str, int durationMs = DefaultPrintSpeed, int brightness = DefaultBrightness)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (str.Length == 0 || str.Length > 0xff) return;
            if (durationMs < 0 || durationMs > 0xffff) return;
            if (brightness < 0 || brightness > 0xff) return;
            if (DisplayBusy)
            {
                // System.Diagnostics.Debug.WriteLine("Display Busy");
                return;
            }

            // Block the display
            int totalDurationMs = str.Length * durationMs;
            _displayFreeTime = DateTime.Now + TimeSpan.FromMilliseconds(totalDurationMs);

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_PRINT_TEXT);
            writer.WriteU16Hex(durationMs);
            writer.WriteU8Hex(brightness);
            writer.WriteString(str);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Sets the brightness of a single pixel.
        /// </summary>
        /// <param name="x">The pixel x coordinate in [0..4].</param>
        /// <param name="y">The pixel y coordinate in [0..4].</param>
        /// <param name="brightness">The pixel brightness in [0..255].</param>
        public void SetPixel(int x, int y, int brightness)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (x < 0 || x > 4) return;
            if (y < 0 || y > 4) return;
            if (brightness < 0 || brightness > 0xff) return;
            if (DisplayBusy)
            {
                // System.Diagnostics.Debug.WriteLine("Display Busy");
                return;
            }

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_SET_PIXEL);
            writer.WriteU8Hex(x);
            writer.WriteU8Hex(y);
            writer.WriteU8Hex(brightness);
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// If a PC speaker is attached, play musical notes through it.
        /// </summary>
        /// <param name="pin">The id of the pin.</param>
        /// <param name="durationMs">The duration of each note.</param>
        /// <param name="tones">The list of musical notes to play.</param>
        public void PlayTones(int pin, int durationMs, int[] tones)
        {
            if (!_port.IsOpen || _status != EDeviceStatus.READY) return;
            if (pin < 0 || pin > 2) return;
            if (durationMs < 0 || durationMs > 0xffff) return;
            if (tones.Length == 0 || tones.Length > 0xff) return;

            if (_curr.Pins[pin].Busy) return;
            _curr.Pins[pin].Busy = true;

            MicroBitMessageWriter writer = new MicroBitMessageWriter();
            writer.WriteChar(Protocol.CMD_PLAY_TONES);
            writer.WriteU8Hex(pin);
            writer.WriteU16Hex(durationMs);
            writer.WriteU8Hex(tones.Length);
            foreach (int tone in tones)
            {
                if (tone >= 0 && tone < 256)
                {
                    writer.WriteU8Hex(tone);
                }
            }
            QueueForSend(writer.ToString());
        }

        /// <summary>
        /// Update the microbit.
        /// </summary>
        public void Update()
        {
            if (_status == EDeviceStatus.READY)
            {
                // Things are good!
            }
            else if (_status == EDeviceStatus.CREATED)
            {
                // If we haven't progressed to the READY state within a short time period,
                // then assume the bit needs flashing.
                if (ShouldFlash())
                {
                    StartFlashing();
                }
            }

            // If we're currently copying the hex file over, don't bother trying to connect over serial.
            if (_status == EDeviceStatus.FLASHING)
            {
                return;
            }

            // If the port is not open, try to open it.
            if (!_port.IsOpen)
            {
                try
                {
                    _port.Open();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            else if (_port.IsOpen)
            {
                // If we haven't sent the bit a message within the last half sec, send a ping.
                // This ensures the bit will continue sending us telemetry.
                if (DateTime.Now - _lastMsgSendTime > TimeSpan.FromSeconds(0.5f))
                {
                    if (_status == EDeviceStatus.CREATED)
                    {
                        Start();
                    }
                    else
                    {
                        Ping();
                    }
                }
            }
        }
    }
#endif
}

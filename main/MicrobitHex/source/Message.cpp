#include "MicroBit.h"

#include "Message.h"

#include <stdlib.h>
#include <string.h>

//============================================================================

static const char ToAscii[] = {'0', '1', '2', '3', '4', '5', '6', '7',
                               '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

//----------------------------------------------------------------------------
// Supports up to base-36
static bool FromAscii(char c, uint8_t& value) {
    // 0-9?
    if (c >= '0' && c <= '9') {
        value = c - '0';
        return true;
    } else {
        // A-Z?
        c &= ~0x20;
        if (c >= 'A' && c <= 'Z') {
            value = 10 + c - 'A';
            return true;
        }
    }
    return false;
}

//----------------------------------------------------------------------------
Message::Message(int length) {
    this->readptr = 0;
    this->writeptr = 0;
    this->initBuffer(length);
}

//----------------------------------------------------------------------------
Message::Message(const char* src) {
    this->readptr = 0;
    this->maxlen = this->writeptr = strlen(src);
    this->initBuffer(src, this->maxlen);
}

//----------------------------------------------------------------------------
Message::Message(const char* chars, int length) {
    this->readptr = 0;
    this->writeptr = length;
    this->initBuffer(chars, length);
}

//----------------------------------------------------------------------------
Message::Message(const Message& msg, bool copy) {
    this->readptr = 0;
    this->writeptr = msg.maxlen;
    if (copy) {
        this->initBuffer(msg.maxlen);
        memcpy(this->buf, msg.buf, msg.maxlen);
    } else {
        this->initBuffer(msg.buf, msg.maxlen);
    }
}

//----------------------------------------------------------------------------
Message::Message() {
    this->allocated = false;
    this->readptr = this->writeptr = this->maxlen = 0;
    this->buf = NULL;
}

//----------------------------------------------------------------------------
Message::~Message() {
    if (this->allocated)
        delete[] this->buf;
}

//----------------------------------------------------------------------------
uint8_t* Message::byteBuffer() const {
    return (uint8_t*)(void*)this->buf;
}

//----------------------------------------------------------------------------
char* Message::charBuffer() const {
    return (char*)(void*)this->buf;
}

//----------------------------------------------------------------------------
int Message::length() const {
    return this->writeptr;
}

//----------------------------------------------------------------------------
int Message::finalize() {
    if (!this->allocated)
        return 0;
    this->buf[this->writeptr] = '\n';
    return this->writeptr + 1;
}

//----------------------------------------------------------------------------
void Message::initBuffer(int length) {
    this->allocated = true;
    this->buf = new char[length + 1];  // include a char for the finalized newline.
    this->maxlen = length;
    memset(this->buf, 0, length + 1);
}

//----------------------------------------------------------------------------
void Message::initBuffer(const char* chars, int length) {
    this->allocated = false;
    this->buf = (char*)chars;
    this->maxlen = this->writeptr = length;
}

//----------------------------------------------------------------------------
void Message::rewind() const {
    this->readptr = 0;
}

//----------------------------------------------------------------------------
bool Message::consume(char value) const {
    if (!this->consumeRaw(value))
        return false;
    return this->consumeSeparator();
}

//----------------------------------------------------------------------------
bool Message::consumeRaw(char value) const {
    if (!this->readable())
        return false;
    if (this->buf[this->readptr] == value) {
        this->readptr++;
        return true;
    }
    return false;
}

//----------------------------------------------------------------------------
bool Message::consume(const char* value) const {
    while (char c = *value++) {
        if (!consumeRaw(c)) {
            return false;
        }
    }
    return this->consumeSeparator();
}

//----------------------------------------------------------------------------
bool Message::readChar(char& value) const {
    if (!this->readCharRaw(value))
        return false;
    return this->consumeSeparator();
}

//----------------------------------------------------------------------------
bool Message::readCharRaw(char& value) const {
    if (!this->readable())
        return false;
    value = this->buf[this->readptr++];
    return true;
}

//----------------------------------------------------------------------------
bool Message::readU16Hex(uint16_t& value) const {
    uint8_t msb;
    uint8_t lsb;

    if (!this->readU8HexRaw(msb))
        return false;
    if (!this->readU8HexRaw(lsb))
        return false;

    value = msb << 8;
    value += lsb;

    return this->consumeSeparator();
}

//----------------------------------------------------------------------------
bool Message::readU8Hex(uint8_t& value) const {
    if (!this->readU8HexRaw(value))
        return false;
    return this->consumeSeparator();
}

//----------------------------------------------------------------------------
bool Message::readU8HexRaw(uint8_t& value) const {
    uint8_t msn;
    uint8_t lsn;

    if (!this->readAsciiNybble(msn))
        return false;
    if (!this->readAsciiNybble(lsn))
        return false;

    value = msn << 4;
    value += lsn;

    return true;
}

//----------------------------------------------------------------------------
bool Message::readChars(char* dst, int bufsize, int& nread) const {
    if (!this->readCharsRaw(dst, bufsize, nread))
        return false;
    return this->consumeSeparator();
}

//----------------------------------------------------------------------------
bool Message::readCharsRaw(char* dst, int bufsize, int& nread) const {
    nread = 0;
    if (!this->readable())
        return false;
    while (bufsize-- > 0) {
        if (!this->readCharRaw(*dst))
            break;
        ++dst;
        ++nread;
    }
    return true;
}

//----------------------------------------------------------------------------
int Message::bytesRemaining() const {
    return this->maxlen - this->readptr;
}

//----------------------------------------------------------------------------
bool Message::readString(ManagedString& str) const {
    if (!this->readable())
        return false;
    uint8_t slen;
    if (!this->readU8HexRaw(slen))
        return false;
    if (slen > this->bytesRemaining())
        return false;
    str = ManagedString(this->buf + this->readptr, slen);
    this->readptr += slen;
    return this->consumeSeparator();
}

//----------------------------------------------------------------------------
bool Message::readImage(MicroBitImage& image) const {
    if (!this->readable())
        return false;
    char packed[5];
    int nread;
    // Image is 5 x 5, black and white. Each row is encoded into a single ascii
    // character as a base-36 encoded set of values.
    if (!this->readCharsRaw(packed, 5, nread) || nread != 5)
        return false;
    if (!this->consumeSeparator())
        return false;
    uint8_t pixels[25];
    for (int y = 0; y < 5; ++y) {
        uint8_t pixel = 0;
        FromAscii(packed[y], pixel);
        for (int x = 0; x < 5; ++x) {
            pixels[y * 5 + (4 - x)] = (pixel & (1 << x)) ? 255 : 0;
        }
    }
    image = MicroBitImage(5, 5, pixels);
    return true;
}

//----------------------------------------------------------------------------
bool Message::readAsciiNybble(uint8_t& value) const {
    if (!this->readable())
        return false;

    char c = this->buf[this->readptr];

    if (!FromAscii(c, value))
        return false;

    ++this->readptr;
    return true;
}

//----------------------------------------------------------------------------
bool Message::writeString(const char* value, bool truncate) {
    int len = strlen(value);
    // NOTE: the value of len is the "ideal" length. If the buffer terminates
    // before the end of the string then it was truncated. Kodu will be
    // careful when reading strings to ensure it reads it properly.
    if (!this->writeAsciiByte(len))
        return false;
    if (this->writeCharsRaw(value, len, truncate))
        return false;
    return this->writeSeparator();
}

//----------------------------------------------------------------------------
bool Message::writeChars(const char* value, int count, bool truncate) {
    if (!this->writable(count)) {
        count = this->maxlen - this->writeptr - 1;
        if (!truncate || !this->writable(count))
            return false;
    }
    memcpy(this->buf + this->writeptr, value, count);
    this->writeptr += count;
    return this->writeSeparator();
}

//----------------------------------------------------------------------------
bool Message::writeCharsRaw(const char* value, int count, bool truncate) {
    if (!this->writable(count)) {
        count = this->maxlen - this->writeptr - 1;
        if (!truncate || !this->writable(count))
            return false;
    }
    memcpy(this->buf + this->writeptr, value, count);
    this->writeptr += count;
    return true;
}

//----------------------------------------------------------------------------
bool Message::writeChar(char value) {
    if (!this->writable(1))
        return false;
    this->buf[this->writeptr++] = value;
    return this->writeSeparator();
}

//----------------------------------------------------------------------------
bool Message::writeCharRaw(char value) {
    if (!this->writable(1))
        return false;
    this->buf[this->writeptr++] = value;
    return true;
}

//----------------------------------------------------------------------------
bool Message::writeU16Hex(uint16_t value) {
    if (!this->writeU16HexRaw(value))
        return false;
    return this->writeSeparator();
}

//----------------------------------------------------------------------------
bool Message::writeU16HexRaw(uint16_t value) {
    if (!this->writable(4))
        return false;
    uint8_t hi = (value >> 8) & 0xFF;
    uint8_t lo = value & 0xFF;
    if (!writeAsciiByte(hi))
        return false;
    if (!writeAsciiByte(lo))
        return false;
    return true;
}

//----------------------------------------------------------------------------
bool Message::writeU8Hex(uint8_t value) {
    if (!this->writeAsciiByte(value))
        return false;
    return this->writeSeparator();
}

//----------------------------------------------------------------------------
bool Message::writeAsciiByte(uint8_t value) {
    if (!this->writable(2))
        return false;
    uint8_t hi = (value >> 4) & 0x0F;
    uint8_t lo = value & 0x0F;
    if (!this->writeCharRaw(ToAscii[hi]))
        return false;
    if (!this->writeCharRaw(ToAscii[lo]))
        return false;
    return true;
}

//----------------------------------------------------------------------------
bool Message::consumeSeparator() const {
    char value = this->buf[this->readptr++];
    return value == '|';
}

//----------------------------------------------------------------------------
bool Message::writeSeparator() {
    this->buf[this->writeptr++] = '|';
    return true;
}

//----------------------------------------------------------------------------
bool Message::readable() const {
    if (this->readptr >= this->writeptr)
        return false;
    return true;
}

//----------------------------------------------------------------------------
bool Message::writable(int length) const {
    if (!this->allocated)
        return false;  // We didn't allocate this buffer
    if (this->buf[this->writeptr] == '\n')
        return false;  // message has been finalized
    if (this->writeptr + length >= this->maxlen)
        return false;  // out of space
    return true;
}

//----------------------------------------------------------------------------
void Message::copyFrom(const Message& msg) {
    if (this->allocated)
        delete[] this->buf;
    this->readptr = 0;
    this->writeptr = msg.maxlen;
    this->initBuffer(msg.maxlen);
    memcpy(this->buf, msg.buf, msg.maxlen);
}

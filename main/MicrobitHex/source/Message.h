#ifndef MESSAGE_H
#define MESSAGE_H

class ManagedString;
class MicroBitImage;

class Message {
   public:
    Message(int length);
    Message(const char* src);
    Message(const char* chars, int length);
    Message(const Message& msg, bool copy);
    Message();
    ~Message();

    uint8_t* byteBuffer() const;
    char* charBuffer() const;
    int length() const;
    int finalize();

    // Read
    void rewind() const;
    bool consume(char value) const;
    bool consume(const char* value) const;
    bool readChar(char& value) const;
    bool readChars(char* dst, int bufsize, int& nread) const;
    bool readU8Hex(uint8_t& value) const;
    bool readU16Hex(uint16_t& value) const;
    bool readString(ManagedString& str) const;
    bool readImage(MicroBitImage& image) const;
    int bytesRemaining() const;

    // Write
    bool writeChar(char value);
    bool writeChars(const char* value, int count, bool truncate = false);
    bool writeString(const char* value, bool truncate = false);
    bool writeU8Hex(uint8_t value);
    bool writeU16Hex(uint16_t value);

    // copy
    void copyFrom(const Message& msg);

   private:
    char* buf;
    bool allocated;
    int maxlen;
    mutable int readptr;
    int writeptr;

    void initBuffer(int length);
    void initBuffer(const char* chars, int length);

    bool readAsciiNybble(uint8_t& value) const;
    bool writeAsciiByte(uint8_t value);

    bool readU8HexRaw(uint8_t& value) const;
    bool readCharRaw(char& value) const;
    bool readCharsRaw(char* dst, int bufsize, int& nread) const;
    bool consumeRaw(char value) const;
    bool consumeSeparator() const;

    bool writeCharsRaw(const char* value, int count, bool truncate);
    bool writeCharRaw(char value);
    bool writeU16HexRaw(uint16_t value);
    bool writeSeparator();

    bool readable() const;
    bool writable(int length) const;
};

#endif  // MESSAGE_H

namespace DelfinForWindows
{
    /// <summary>
    /// Represents the header at the beginning of the encrypted file, including methods for converting the header to and from buffers.
    /// </summary>
    class Header
    {
        public const int CURRENT_VERSION = 0;

        public bool IsComplete;
        public bool IsUnsupported;
        public int FileSize;
        public byte HeaderVersion;
        public int HeaderSize;

        private short byteScan;

        // Designed to be extensible, the header contains one constructor that initializes all current-version defaults.
        public Header()
        {
            IsComplete = false;
            IsUnsupported = false;
            HeaderVersion = 0;
            byteScan = 0;
            HeaderSize = 5;
        }

        /// <summary>
        /// When constructing a header from a byte buffer, feed the next byte via this method.
        /// </summary>
        /// <param name="b">The next byte in the buffer.</param>
        public void AddByte(byte b)
        {
            // quit if unsupported header
            if(IsUnsupported)
            {
                return;
            }

            // 0 (1 byte): header version
            if (byteScan == 0)
            {
                // assign version and check support
                if ((HeaderVersion = b) > CURRENT_VERSION)
                {
                    IsUnsupported = true;
                    return;
                }
                byteScan = 1; // equivalent to byteScan++ when byteScan is 0
                return;
            }

            if (HeaderVersion == 0) // Versions 0.6+ use HV0
            {
                // 1-4 (4 bytes): fileSize
                if (byteScan < 5)
                {
                    FileSize <<= 8;
                    FileSize |= b;
                    byteScan++;
                    if (byteScan == 5)
                    {
                        HeaderSize = 5;
                        IsComplete = true;
                    }
                }
            }
            // as new versions of the header appear, they will show up as additional ifs here
        }

        /// <summary>
        /// Converts the header to a byte buffer.
        /// </summary>
        public byte[] ToBuffer()
        {
            // HV0:
            //  byte 0: header version (0x00)
            //  bytes 1-4: filesize (int32)
            byte[] buffer = new byte[5];
            buffer[0] = 0;
            buffer[1] = (byte)((FileSize >> 24) & 255);
            buffer[2] = (byte)((FileSize >> 16) & 255);
            buffer[3] = (byte)((FileSize >> 8) & 255);
            buffer[4] = (byte)(FileSize & 255);
            return buffer;
        }
    }
}
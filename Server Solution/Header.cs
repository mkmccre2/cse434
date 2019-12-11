using System;

namespace ServerEF
{
    public class Header
    {
        public char magic1 = 'M';
        public char magic2 = 'M';
        public int opcode;
        public int payload_len;
        public int token;
        public int msg_id;
        public string payload;
        public byte[] IV;
    }
}


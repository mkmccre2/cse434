using System;
namespace Client
{
    public class Opcode
    {
        public const int RESET = 0;
        public const int OPCODE_MUST_LOGIN_FIRST_ERROR = 1;
        public const int OPCODE_LOGIN = 2;
        public const int OPCODE_SUCCESSFUL_LOGIN_ACK = 3;
        public const int OPCODE_FAILED_LOGIN_ACK = 4;
        public const int OPCODE_SUBSCRIBE = 5;
        public const int OPCODE_SUCCESSFUL_SUBSCRIBE_ACK = 6;
        public const int OPCODE_FAILED_SUBSCRIBE_ACK = 7;
        public const int OPCODE_UNSUBSCRIBE = 8;
        public const int OPCODE_SUCCESSFUL_UNSUBSCRIBE_ACK = 9;
        public const int OPCODE_FAILED_UNSUBSCRIBE_ACK = 10;
        public const int OPCODE_POST = 11;
        public const int OPCODE_POST_ACK = 12;
        public const int OPCODE_FORWARD = 13;
        public const int OPCODE_FORWARD_ACK = 14;
        public const int OPCODE_RETRIEVE = 15;
        public const int OPCODE_RETRIEVE_ACK = 16;
        public const int OPCODE_END_RETRIEVE_ACK = 17;
        public const int OPCODE_LOGOUT = 18;
        public const int OPCODE_LOGOUT_ACK = 19;
    }
}

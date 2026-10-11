using System;

namespace FunctionsV2.Sys_SymPay.Exceptions
{
    public class SymPayBusinessException : Exception
    {
        public SymPayBusinessException(string message)
            : base(message)
        {
        }
    }

    public class SymPayRetryableException : Exception
    {
        public SymPayRetryableException(string message)
            : base(message)
        {
        }
    }
}

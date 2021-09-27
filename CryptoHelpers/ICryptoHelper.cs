using System;

namespace CryptoHelpers
{
    public interface ICryptoHelper : IDisposable
    {
        byte[] Sign(byte[] data, string certParamValue);
        bool Verify(byte[] signature, string certParamValue);
    }
}
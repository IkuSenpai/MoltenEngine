﻿namespace Molten.Graphics
{
    public class ConstantBufferInfo
    {
        public string Name;

        public ConstantBufferType Type;

        public ConstantBufferFlags Flags;

        /// <summary>
        /// Size in bytes.
        /// </summary>
        public uint Size;

        public List<ConstantBufferVariableInfo> Variables { get; } = new List<ConstantBufferVariableInfo>();

        public bool hasFlags(ConstantBufferFlags flags)
        {
            return (Flags & flags) == flags;
        }
    }
}

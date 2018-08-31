﻿namespace packer
{
    public interface IStrategy
    {
        void Work(string source, string destination);
        string Name { get; }
    }
}

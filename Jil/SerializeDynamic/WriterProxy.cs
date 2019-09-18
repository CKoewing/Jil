﻿using System.IO;

namespace Jil.SerializeDynamic
{
    internal ref struct WriterProxy
    {
        private TextWriter Inner;

        public void Init(TextWriter inner)
        {
            Inner = inner;
        }

        public void Write(char c)
        {
            Inner.Write(c);
        }

        public void Write(string str)
        {
            Inner.Write(str);
        }

        public TextWriter AsWriter()
        {
            return Inner;
        }
    }
}

﻿#if BUFFER_AND_SEQUENCE
using System;
using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Jil.Serialize
{
    internal ref partial struct ThunkWriter
    {
        IBufferWriter<char> Builder;
        Span<char> Current;
        int Start;

        private void AdvanceAndAcquire()
        {
            var toFlush = Start;
            if(toFlush == 0)
            {
                // implies what we were given was too small
                var requestLength = Current.Length * 2;
                Builder.Advance(0);
                Current = Builder.GetSpan(requestLength);
            }
            else
            {
                Builder.Advance(toFlush);
                Current = Builder.GetSpan();
            }

            Start = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init(IBufferWriter<char> buffer)
        {
            Builder = buffer;
            Start = 0;
            Current = Builder.GetSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(float f)
        {
            tryAgain:
            if(!f.TryFormat(Current.Slice(Start), out var chars, provider: CultureInfo.InvariantCulture))
            {
                AdvanceAndAcquire();
                goto tryAgain;
            }

            Start += chars;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double d)
        {
            tryAgain:
            if (!d.TryFormat(Current.Slice(Start), out var chars, provider: CultureInfo.InvariantCulture))
            {
                AdvanceAndAcquire();
                goto tryAgain;
            }

            Start += chars;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(decimal m)
        {
            tryAgain:
            if (!m.TryFormat(Current.Slice(Start), out var chars, provider: CultureInfo.InvariantCulture))
            {
                AdvanceAndAcquire();
                goto tryAgain;
            }

            Start += chars;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(char[] ch, int startIx, int len)
        {
            var toCopy = ch.AsSpan().Slice(startIx, len);

            WriteSpan(toCopy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteSpan(ReadOnlySpan<char> toCopy)
        {
            while (!toCopy.IsEmpty)
            {
                tryAgain:
                var available = Current.Length - Start;
                if (available == 0)
                {
                    AdvanceAndAcquire();
                    goto tryAgain;
                }

                var copyLen = Math.Min(available, toCopy.Length);
                var subset = toCopy.Slice(0, copyLen);

                subset.CopyTo(Current.Slice(Start));
                toCopy = toCopy.Slice(copyLen);

                Start += copyLen;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(char ch)
        {
            if(Start == Current.Length)
            {
                AdvanceAndAcquire();
            }

            Current[Start] = ch;
            Start++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteCommonConstant(ConstantString_Common str)
        {
            var asUShort = (ushort)str;
            var ix = asUShort >> 8;
            var len = asUShort & 0xFF;

            // todo: maybe take these spans at alloc time, so we can skip the AsSpan()s during a call?
            var subset = ThunkWriterCharArrays.ConstantString_Common_Chars.AsSpan().Slice(ix, len);

            WriteSpan(subset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFormattingConstant(ConstantString_Formatting str)
        {
            var asUShort = (ushort)str;
            var ix = (asUShort >> 8);
            var len = asUShort & 0xFF;

            // todo: maybe take these spans at alloc time, so we can skip the AsSpan()s during a call?
            var subset = ThunkWriterCharArrays.ConstantString_Formatting_Chars.AsSpan().Slice(ix, len);

            WriteSpan(subset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMinConstant(ConstantString_Min str)
        {
            var asUShort = (ushort)str;
            var ix = (asUShort >> 8);
            var len = asUShort & 0xFF;

            // todo: maybe take these spans at alloc time, so we can skip the AsSpan()s during a call?
            var subset = ThunkWriterCharArrays.ConstantString_Min_Chars.AsSpan().Slice(ix, len);

            WriteSpan(subset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteValueConstant(ConstantString_Value str)
        {
            var asUShort = (ushort)str;
            var ix = (asUShort >> 8);
            var len = asUShort & 0xFF;

            // todo: maybe take these spans at alloc time, so we can skip the AsSpan()s during a call?
            var subset = ThunkWriterCharArrays.ConstantString_Value_Chars.AsSpan().Slice(ix, len);

            WriteSpan(subset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write000EscapeConstant(ConstantString_000Escape str)
        {
            var ix = (byte)str;

            // todo: maybe take these spans at alloc time, so we can skip the AsSpan()s during a call?
            WriteSpan(ThunkWriterCharArrays.Escape000Prefix.AsSpan());
            Write(ThunkWriterCharArrays.ConstantString_000Escape_Chars[ix]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write001EscapeConstant(ConstantString_001Escape str)
        {
            var ix = (byte)str;

            WriteSpan(ThunkWriterCharArrays.Escape001Prefix.AsSpan());
            Write(ThunkWriterCharArrays.ConstantString_001Escape_Chars[ix]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDayOfWeek(ConstantString_DaysOfWeek str)
        {
            var ix = (byte)str;

            var subset = ThunkWriterCharArrays.ConstantString_DaysOfWeek.AsSpan().Slice(ix, 3);

            WriteSpan(subset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(string strRef)
        {
            WriteSpan(strRef.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End()
        {
            Builder.Advance(Start);
            Current = Span<char>.Empty;
            Start = 0;
            Builder = null;
        }
    }
}
#endif
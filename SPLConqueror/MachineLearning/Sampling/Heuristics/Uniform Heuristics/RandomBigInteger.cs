using System;
using System.Numerics;

namespace System.Numerics
{
    class RandomBigInteger : Random
    {
        Random random;
        public RandomBigInteger() : base()
        {
        }

        public RandomBigInteger(int Seed) : base(Seed)
        {
            random = new Random(Seed);
        }

        /// <summary>
        /// Generates a random positive BigInteger between 0 and 2^bitLength (non-inclusive).
        /// </summary>
        /// <param name="bitLength">The number of random bits to generate.</param>
        /// <returns>A random positive BigInteger between 0 and 2^bitLength (non-inclusive).</returns>
        public BigInteger NextBigInteger(int bitLength)
        {
            if (bitLength < 1) return BigInteger.Zero;

            int bytes = bitLength / 8;
            int bits = bitLength % 8;

            // Generates enough random bytes to cover our bits.
            byte[] bs = new byte[bytes + 1];
            NextBytes(bs);

            // Mask out the unnecessary bits.
            byte mask = (byte)(0xFF >> (8 - bits));
            bs[bs.Length - 1] &= mask;

            return new BigInteger(bs);
        }

        /// <summary>
        /// Generates a random BigInteger between start and end (non-inclusive).
        /// </summary>
        /// <param name="start">The lower bound.</param>
        /// <param name="end">The upper bound (non-inclusive).</param>
        /// <returns>A random BigInteger between start and end (non-inclusive)</returns>
        public BigInteger NextBigInteger(BigInteger start, BigInteger end)
        {
            if (start == end) return start;

            BigInteger res = end;

            // Swap start and end if given in reverse order.
            if (start > end)
            {
                end = start;
                start = res;
                res = end - start;
            }
            else
                // The distance between start and end to generate a random BigIntger between 0 and (end-start) (non-inclusive).
                res -= start;

            byte[] bs = res.ToByteArray();
            if (bs.Length < 9)
            {
                return Next(0, (Int64)res);
            }

            // Count the number of bits necessary for res.
            int bits = 8;
            byte mask = 0x7F;
            while ((bs[bs.Length - 1] & mask) == bs[bs.Length - 1])
            {
                bits--;
                mask >>= 1;
            }
            bits += 8 * bs.Length;

            // Generate a random BigInteger that is the first power of 2 larger than res, 
            // then scale the range down to the size of res,
            // finally add start back on to shift back to the desired range and return.
            return ((NextBigInteger(bits + 1) * res) / BigInteger.Pow(2, bits + 1)) + start;
        }

        public long Next(long min, long max)
        {

            if (max <= min)
                throw new ArgumentOutOfRangeException("max", "max must be > min!");

            ulong uRange = (ulong)(max - min);
            ulong ulongRand;
            do
            {
                byte[] buf = new byte[8];
                random.NextBytes(buf);
                ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
            } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

            return (long)(ulongRand % uRange) + min;

        }
    }
}

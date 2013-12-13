﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using BitmapProcessing;

namespace ImageHiding
{
    public class Encrypt
    {
        private string secretMessage;
        private Bitmap coverImageBitmap;
        private FastBitmap coverImage;
        byte[] MessageBitString;
        private int NumberOfLSB;
        int BitPartitions;

        public Encrypt(string secretMessage, string coverImageDirectory)
        {
            this.secretMessage = secretMessage;
            coverImageBitmap = new Bitmap(coverImageDirectory); // Creating Ordinary Bitmap to Load Image from Path 
            coverImage = new FastBitmap(coverImageBitmap);  // FastBitmap is a costum created unsafe bitmap which allow faster access by manual locking/unlocking
            NumberOfLSB = 4;
            BitPartitions = (secretMessage.Length + NumberOfLSB - 1) / NumberOfLSB;
        }

        public string Run()
        {
            MessageBitString = new BitArray(PartitionMessage(secretMessage));
            string outputHash = "";
            int x0 = 0, a = 1, b = 1, c = 1;// x0 , xi+1 = a(xi+1)^b +c passed by reference
            GenerateSequence(ref x0, ref a, ref b, ref c);
            ReplacePixels(x0, a, b, c);
            outputHash = HashRecurrence(x0, a, b, c, secretMessage.Length);
            return outputHash;
        }

        private string HashRecurrence(int a, int b, int c, int d, int l)
        {
            List<int> Nums = new List<int>();
            Nums.Add(a); Nums.Add(b); Nums.Add(c); Nums.Add(d); Nums.Add(l);
            string hashed = "";
            for (int j = 0; j < 5; j++)
            {
                long Pow = (long)Math.Pow(2.0, 32.0);
                long i = Nums[j] * 2654435761;
                long div = i / Pow;
                long rem = i % Pow;

                hashed += div;
                hashed += '-';
                hashed += rem;
                hashed += '-';
            }
            return hashed;
        }
        private byte[] PartitionMessage(string secretMessage)
        {
            int len = secretMessage.Length;
            int partSize = (8 + NumberOfLSB - 1) / NumberOfLSB;
            byte[] bitString = new byte[len * partSize + len];


            for (int i = 0; i < len; i++)
                for (int bit = 0; bit < 8; bit++)
                    bitString[i * bit / partSize] = (byte)(((int)secretMessage[i] & (1 << bit)));



            return bitString;
        }

        private void GenerateSequence(ref int x0, ref int a, ref int b, ref int c)
        {
            // here we will implement the optimal seq.
            // for now x0 = 0 , xi+1 = (1xi^1+1)%(m*n)
        }

        private void ReplacePixels(int x0, int a, int b, int c)
        {
            Bitmap stegoImage = new Bitmap(coverImageBitmap);
            int n = stegoImage.Height;
            int m = stegoImage.Width;
            int MOD = m * n;
            int index = x0;
            for (int i = 0; i < MessageBitString.Length; i++)
            {
                int x = index / n;
                int y = index % m;
                int newLSB = ReplaceLSB(0, MessageBitString[i]);
                Color OldColor = stegoImage.GetPixel(x, y);
                Color NewColor = Color.FromArgb(ReplaceLSB(OldColor.ToArgb(), (byte)newLSB));
                stegoImage.SetPixel(x, y, NewColor);
                index = (((a % MOD * (int)powerMod(ref index, b, ref MOD)) % MOD) + c % MOD) % MOD;
            }
            stegoImage.Dispose();
            //coverImage.LockImage();
            //coverImage.UnlockImage();
            //coverImageBitmap.Dispose(); // Clear Image from Memory

        }

        private long powerMod(ref int Number, int Power, ref int MOD)
        {
            if (Power == 0) return 1;
            if (Power % 2 == 1)
                return (powerMod(ref Number, Power - 1, ref MOD) % MOD * (long)Number % MOD) % MOD;
            else
            {
                long Ret = powerMod(ref Number, Power - 1, ref MOD);
                Ret *= Ret;
                return Ret%MOD;
            }

        }

        private int ReplaceLSB(int original, byte pat)
        {
            int ret = original;
            ret = ((ret & (~((1 << (NumberOfLSB + 1)) - 1)))); // clear k bits
            ret |= ((int)pat & ((1 << (NumberOfLSB + 1)) - 1)); // set the k bits
            return ret;
        }

    }
}

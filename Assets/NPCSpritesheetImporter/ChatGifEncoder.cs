// UnityGifEncoder.cs
// Pure C# GIF encoder for Unity.
// No System.Drawing, no third-party libraries.
//
// Usage:
// GifEncoder.SaveGif(textures, path, delayMs: 100);
//
// Notes:
// - All frames must be the same size.
// - Uses a global 256-color palette generated from all frames.
// - Transparency is supported for pixels with alpha < 128.
// - Works in Unity Editor + Runtime.
//
// Tested with Unity 6 / .NET Standard 2.1 compatible APIs.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ChatGifEncoder {
    const int ColorDepth = 8;
    const int PaletteSize = 256;
    const int TransparentIndex = 0;

    class IndexedFrame {
        public byte[] Pixels;
    }

    struct Color32RGB {
        public byte r;
        public byte g;
        public byte b;

        public Color32RGB(byte r, byte g, byte b) {
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }

    public static void SaveGif(List<Texture2D> frames, string path, int delayMs = 100, bool loop = true) {
        if (frames == null || frames.Count == 0)
            throw new Exception("No frames provided.");

        int width = frames[0].width;
        int height = frames[0].height;

        foreach (var tex in frames) {
            if (tex.width != width || tex.height != height)
                throw new Exception("All frames must be the same size.");
        }

        // Build palette from all frames
        List<Color32RGB> palette = BuildPalette(frames);

        // Convert frames to indexed color
        List<IndexedFrame> indexedFrames = new List<IndexedFrame>();

        foreach (var tex in frames) {
            indexedFrames.Add(ConvertToIndexed(tex, palette));
        }

        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs)) {
            WriteHeader(bw);
            WriteLogicalScreenDescriptor(bw, width, height);
            WriteGlobalColorTable(bw, palette);

            if (loop)
                WriteNetscapeLoopExtension(bw);

            foreach (var frame in indexedFrames) {
                WriteGraphicControlExtension(bw, delayMs);
                WriteImageDescriptor(bw, width, height);
                WriteImageData(bw, frame.Pixels);
            }

            // GIF Trailer
            bw.Write((byte)0x3B);
        }
    }

    static void WriteHeader(BinaryWriter bw) {
        bw.Write(System.Text.Encoding.ASCII.GetBytes("GIF89a"));
    }

    static void WriteLogicalScreenDescriptor(BinaryWriter bw, int width, int height) {
        WriteShort(bw, width);
        WriteShort(bw, height);

        // Global color table flag set
        // 8-bit color depth
        bw.Write((byte)0xF7);

        // Background color index
        bw.Write((byte)0);

        // Pixel aspect ratio
        bw.Write((byte)0);
    }

    static void WriteGlobalColorTable(BinaryWriter bw, List<Color32RGB> palette) {
        for (int i = 0; i < PaletteSize; i++) {
            Color32RGB c;

            if (i < palette.Count)
                c = palette[i];
            else
                c = new Color32RGB(0, 0, 0);

            bw.Write(c.r);
            bw.Write(c.g);
            bw.Write(c.b);
        }
    }

    static void WriteGraphicControlExtension(BinaryWriter bw, int delayMs) {
        bw.Write((byte)0x21);
        bw.Write((byte)0xF9);
        bw.Write((byte)4);

        // Transparency enabled
        bw.Write((byte)0x01);

        // Delay in 1/100 sec
        WriteShort(bw, delayMs / 10);

        bw.Write((byte)TransparentIndex);

        bw.Write((byte)0);
    }

    static void WriteImageDescriptor(BinaryWriter bw, int width, int height) {
        bw.Write((byte)0x2C);

        WriteShort(bw, 0);
        WriteShort(bw, 0);

        WriteShort(bw, width);
        WriteShort(bw, height);

        // No local color table
        bw.Write((byte)0);
    }

    static void WriteImageData(BinaryWriter bw, byte[] indexedPixels) {
        int minCodeSize = 8;
        bw.Write((byte)minCodeSize);

        byte[] compressed = LzwCompress(indexedPixels, minCodeSize);

        int offset = 0;

        while (offset < compressed.Length) {
            int blockSize = Mathf.Min(255, compressed.Length - offset);

            bw.Write((byte)blockSize);
            bw.Write(compressed, offset, blockSize);

            offset += blockSize;
        }

        // End block
        bw.Write((byte)0);
    }

    static void WriteNetscapeLoopExtension(BinaryWriter bw) {
        bw.Write((byte)0x21);
        bw.Write((byte)0xFF);
        bw.Write((byte)11);

        bw.Write(System.Text.Encoding.ASCII.GetBytes("NETSCAPE2.0"));

        bw.Write((byte)3);
        bw.Write((byte)1);

        // 0 = infinite loop
        WriteShort(bw, 0);

        bw.Write((byte)0);
    }

    static void WriteShort(BinaryWriter bw, int value) {
        bw.Write((byte)(value & 0xFF));
        bw.Write((byte)((value >> 8) & 0xFF));
    }

    // =========================================================
    // Palette Generation
    // =========================================================

    static List<Color32RGB> BuildPalette(List<Texture2D> textures) {
        Dictionary<int, int> histogram = new Dictionary<int, int>();

        foreach (var tex in textures) {
            Color32[] pixels = tex.GetPixels32();

            foreach (var p in pixels) {
                if (p.a < 128)
                    continue;

                // Quantize to 5 bits/channel
                int r = p.r >> 3;
                int g = p.g >> 3;
                int b = p.b >> 3;

                int key = (r << 10) | (g << 5) | b;

                if (!histogram.ContainsKey(key))
                    histogram[key] = 0;

                histogram[key]++;
            }
        }

        List<KeyValuePair<int, int>> sorted =
            new List<KeyValuePair<int, int>>(histogram);

        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        List<Color32RGB> palette = new List<Color32RGB>();

        // Index 0 reserved for transparency
        palette.Add(new Color32RGB(0, 0, 0));

        int count = Mathf.Min(255, sorted.Count);

        for (int i = 0; i < count; i++) {
            int key = sorted[i].Key;

            byte r = (byte)(((key >> 10) & 31) << 3);
            byte g = (byte)(((key >> 5) & 31) << 3);
            byte b = (byte)((key & 31) << 3);

            palette.Add(new Color32RGB(r, g, b));
        }

        return palette;
    }

    static IndexedFrame ConvertToIndexed(Texture2D tex, List<Color32RGB> palette) {
        Color32[] pixels = tex.GetPixels32();

        byte[] indexed = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i++) {
            Color32 p = pixels[i];

            if (p.a < 128) {
                indexed[i] = TransparentIndex;
                continue;
            }

            int bestIndex = 1;
            int bestDist = int.MaxValue;

            for (int j = 1; j < palette.Count; j++) {
                Color32RGB c = palette[j];

                int dr = p.r - c.r;
                int dg = p.g - c.g;
                int db = p.b - c.b;

                int dist = dr * dr + dg * dg + db * db;

                if (dist < bestDist) {
                    bestDist = dist;
                    bestIndex = j;
                }
            }

            indexed[i] = (byte)bestIndex;
        }

        return new IndexedFrame {
            Pixels = indexed
        };
    }

    // =========================================================
    // GIF LZW Compression
    // =========================================================

    static byte[] LzwCompress(byte[] data, int colorDepth) {
        int clearCode = 1 << colorDepth;
        int endCode = clearCode + 1;
        int nextCode = endCode + 1;

        int codeSize = colorDepth + 1;

        Dictionary<string, int> dict = new Dictionary<string, int>();

        for (int i = 0; i < clearCode; i++)
            dict[((char)i).ToString()] = i;

        List<int> codes = new List<int>();

        codes.Add(clearCode);

        string current = ((char)data[0]).ToString();

        for (int i = 1; i < data.Length; i++) {
            char c = (char)data[i];
            string combined = current + c;

            if (dict.ContainsKey(combined)) {
                current = combined;
            } else {
                codes.Add(dict[current]);

                if (nextCode < 4096) {
                    dict[combined] = nextCode++;
                }

                if (nextCode >= (1 << codeSize) && codeSize < 12) {
                    codeSize++;
                }

                current = c.ToString();
            }
        }

        codes.Add(dict[current]);
        codes.Add(endCode);

        return PackCodes(codes, colorDepth);
    }

    static byte[] PackCodes(List<int> codes, int colorDepth) {
        int clearCode = 1 << colorDepth;
        int endCode = clearCode + 1;

        int codeSize = colorDepth + 1;
        int nextCode = endCode + 1;

        List<byte> output = new List<byte>();

        int cur = 0;
        int bits = 0;

        void WriteCode(int code) {
            cur |= (code << bits);
            bits += codeSize;

            while (bits >= 8) {
                output.Add((byte)(cur & 0xFF));
                cur >>= 8;
                bits -= 8;
            }
        }

        foreach (int code in codes) {
            WriteCode(code);

            if (code != clearCode && code != endCode) {
                nextCode++;

                if (nextCode >= (1 << codeSize) && codeSize < 12) {
                    codeSize++;
                }
            }
        }

        if (bits > 0)
            output.Add((byte)(cur & 0xFF));

        return output.ToArray();
    }
}
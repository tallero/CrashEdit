﻿using System;

namespace Crash
{
    public struct OldModelTexture : OldModelStruct
    {
        public static OldModelTexture Load(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length != 12)
                throw new ArgumentException("Value must be 12 bytes long.", "data");
            byte r = data[0];
            byte g = data[1];
            byte b = data[2];
            byte blendmode = (byte)((data[3] >> 5) & 0x3);
            byte clutx = (byte)(data[3] & 0xF);
            int eid = BitConv.FromInt32(data,4);
            int texinfo = BitConv.FromInt32(data,8);
            int uvindex = (texinfo >> 22) & 0x3FF;
            byte colormode = (byte)(texinfo >> 20 & 3);
            byte segment = (byte)(texinfo >> 18 & 3);
            byte xoffu = (byte)(texinfo >> 13 & 0x1F);
            byte cluty = (byte)(texinfo >> 6 & 0x7F);
            byte yoffu = (byte)(texinfo & 0x1F);
            return new OldModelTexture(uvindex,clutx,cluty,xoffu,yoffu,colormode,blendmode,segment,r,g,b,eid);
        }
        public OldModelTexture(int uvindex,byte clutx,byte cluty,byte xoffu,byte yoffu,byte colormode,byte blendmode,byte segment,byte r,byte g,byte b,int eid)
        {
            UVIndex = uvindex;
            ClutX = clutx;
            ClutY = cluty;
            XOffU = xoffu;
            YOffU = yoffu;
            Segment = segment;
            BlendMode = blendmode;
            ColorMode = colormode;
            R = r;
            G = g;
            B = b;
            EID = eid;
        }
        
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public int EID { get; }

        public byte ColorMode { get; set; }
        public int UVIndex { get; set; }
        public byte ClutX { get; set; } // 16-color (32-byte) segments
        public byte ClutY { get; set; }
        public byte XOffU { get; set; }
        public byte YOffU { get; set; }
        public byte BlendMode { get; set; }
        public byte Segment { get; set; }

        public float PageWidth => 256 << (2-ColorMode);
        public int XOff => ((64 << (2-ColorMode)) * Segment) + ((2 << (2-ColorMode)) * XOffU);
        public int YOff => YOffU * 4;
        public int Left => Math.Min(U1, Math.Min(U2, U3)) + XOff;
        public int Top =>  Math.Min(V1, Math.Min(V2, V3)) + YOff;
        public int Width =>  4 << (UVIndex % 5);
        public int Height => 4 << (UVIndex / 5 % 5);
        public int FlipWinding => UVIndex / 25;
        public int U1 =>  Width * (0x30FF0C >> FlipWinding & 1);
        public int V1 => Height * (0xF3CC30 >> FlipWinding & 1);
        public int U2 =>  Width * (0x8799E1 >> FlipWinding & 1);
        public int V2 => Height * (0x9E7186 >> FlipWinding & 1);
        public int U3 =>  Width * (0x4B66D2 >> FlipWinding & 1);
        public int V3 => Height * (0x6DB249 >> FlipWinding & 1);

        public float X1 => (U1 + XOff) / PageWidth;
        public float X2 => (U2 + XOff) / PageWidth;
        public float X3 => (U3 + XOff) / PageWidth;
        public float Y1 => (V1 + YOff) / 128F;
        public float Y2 => (V2 + YOff) / 128F;
        public float Y3 => (V3 + YOff) / 128F;

        public byte[] Save()
        {
            byte[] result = new byte[8];
            result[0] = 0;
            result[1] = 0;
            result[2] = 0;
            result[3] = (byte)(0x80 | (BlendMode << 5) | ClutX);
            uint texinfo = ((uint)UVIndex << 22) | ((uint)ColorMode << 20) | ((uint)Segment << 18) | ((uint)XOffU << 13) | ((uint)ClutY << 6) | YOffU;
            BitConv.ToInt32(result, 4, (int)texinfo);
            return result;
        }
    }

}

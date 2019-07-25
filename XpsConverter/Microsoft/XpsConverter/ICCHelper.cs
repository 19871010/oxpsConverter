using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.XpsConverter
{
    internal static class ICCHelper
    {
        [DllImport("mscms.dll", SetLastError = true)]
        private static extern IntPtr OpenColorProfile(ref PROFILE pProfile, uint dwDesiredAccess, uint dwShareMode, uint dwCreationMode);

        [DllImport("mscms.dll", SetLastError = true)]
        private static extern bool CloseColorProfile(IntPtr phProfile);

        [DllImport("mscms.dll", SetLastError = true)]
        private static extern bool GetColorProfileHeader(IntPtr phProfile, out PROFILEHEADER pHeader);

        public static int GetColorProfileChannelCount(Stream colorProfileStream)
        {
            int result = 0;
            IntPtr intPtr = IntPtr.Zero;
            byte[] array = new byte[colorProfileStream.Length];
            int cbDataSize = colorProfileStream.Read(array, 0, array.Length);
            GCHandle gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                PROFILE profile = default;
                profile.dwType = 2u;
                profile.pProfileData = gchandle.AddrOfPinnedObject();
                profile.cbDataSize = (uint)cbDataSize;
                intPtr = OpenColorProfile(ref profile, 1u, 1u, 3u);
                if (intPtr != IntPtr.Zero)
                {
                    if (GetColorProfileHeader(intPtr, out PROFILEHEADER profileheader))
                    {
                        result = ColorSpaceToChannelCount(profileheader.phDataColorSpace);
                    }
                }
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                {
                    CloseColorProfile(intPtr);
                }
                gchandle.Free();
            }
            return result;
        }

        private static int ColorSpaceToChannelCount(ColorSpace phDataColorSpace)
        {
            if (phDataColorSpace > ColorSpace.SPACE_CMYK)
            {
                if (phDataColorSpace <= ColorSpace.SPACE_HSV)
                {
                    if (phDataColorSpace <= ColorSpace.SPACE_F_CHANNEL)
                    {
                        if (phDataColorSpace == ColorSpace.SPACE_D_CHANNEL)
                        {
                            return 13;
                        }
                        if (phDataColorSpace == ColorSpace.SPACE_E_CHANNEL)
                        {
                            return 14;
                        }
                        if (phDataColorSpace != ColorSpace.SPACE_F_CHANNEL)
                        {
                            goto IL_1D0;
                        }
                        return 15;
                    }
                    else
                    {
                        if (phDataColorSpace == ColorSpace.SPACE_GRAY)
                        {
                            return 1;
                        }
                        if (phDataColorSpace != ColorSpace.SPACE_HLS && phDataColorSpace != ColorSpace.SPACE_HSV)
                        {
                            goto IL_1D0;
                        }
                    }
                }
                else if (phDataColorSpace <= ColorSpace.SPACE_RGB)
                {
                    if (phDataColorSpace != ColorSpace.SPACE_Lab && phDataColorSpace != ColorSpace.SPACE_Luv)
                    {
                        if (phDataColorSpace != ColorSpace.SPACE_RGB)
                        {
                            goto IL_1D0;
                        }
                        return 3;
                    }
                }
                else if (phDataColorSpace <= ColorSpace.SPACE_YCbCr)
                {
                    if (phDataColorSpace != ColorSpace.SPACE_XYZ && phDataColorSpace != ColorSpace.SPACE_YCbCr)
                    {
                        goto IL_1D0;
                    }
                }
                else if (phDataColorSpace != ColorSpace.SPACE_Yxy)
                {
                    if (phDataColorSpace != ColorSpace.SPACE_sRGB)
                    {
                        goto IL_1D0;
                    }
                    return 3;
                }
                return 0;
            }
            if (phDataColorSpace <= ColorSpace.SPACE_7_CHANNEL)
            {
                if (phDataColorSpace <= ColorSpace.SPACE_4_CHANNEL)
                {
                    if (phDataColorSpace == ColorSpace.SPACE_2_CHANNEL)
                    {
                        return 2;
                    }
                    if (phDataColorSpace == ColorSpace.SPACE_3_CHANNEL)
                    {
                        return 3;
                    }
                    if (phDataColorSpace == ColorSpace.SPACE_4_CHANNEL)
                    {
                        return 4;
                    }
                }
                else
                {
                    if (phDataColorSpace == ColorSpace.SPACE_5_CHANNEL)
                    {
                        return 5;
                    }
                    if (phDataColorSpace == ColorSpace.SPACE_6_CHANNEL)
                    {
                        return 6;
                    }
                    if (phDataColorSpace == ColorSpace.SPACE_7_CHANNEL)
                    {
                        return 7;
                    }
                }
            }
            else if (phDataColorSpace <= ColorSpace.SPACE_A_CHANNEL)
            {
                if (phDataColorSpace == ColorSpace.SPACE_8_CHANNEL)
                {
                    return 8;
                }
                if (phDataColorSpace == ColorSpace.SPACE_9_CHANNEL)
                {
                    return 9;
                }
                if (phDataColorSpace == ColorSpace.SPACE_A_CHANNEL)
                {
                    return 10;
                }
            }
            else if (phDataColorSpace <= ColorSpace.SPACE_C_CHANNEL)
            {
                if (phDataColorSpace == ColorSpace.SPACE_B_CHANNEL)
                {
                    return 11;
                }
                if (phDataColorSpace == ColorSpace.SPACE_C_CHANNEL)
                {
                    return 12;
                }
            }
            else
            {
                if (phDataColorSpace == ColorSpace.SPACE_CMY)
                {
                    return 3;
                }
                if (phDataColorSpace == ColorSpace.SPACE_CMYK)
                {
                    return 4;
                }
            }
        IL_1D0:
            return 0;
        }

        private const uint PROFILE_FILENAME = 1u;
        private const uint PROFILE_MEMBUFFER = 2u;
        private const uint PROFILE_READ = 1u;
        private const uint FILE_SHARE_READ = 1u;
        private const uint OPEN_EXISTING = 3u;

        private enum ColorSpace : uint
        {
            SPACE_XYZ = 1482250784u,
            SPACE_Lab = 1281450528u,
            SPACE_Luv = 1282766368u,
            SPACE_YCbCr = 1497588338u,
            SPACE_Yxy = 1501067552u,
            SPACE_RGB = 1380401696u,
            SPACE_GRAY = 1196573017u,
            SPACE_HSV = 1213421088u,
            SPACE_HLS = 1212961568u,
            SPACE_CMYK = 1129142603u,
            SPACE_CMY = 1129142560u,
            SPACE_2_CHANNEL = 843271250u,
            SPACE_3_CHANNEL = 860048466u,
            SPACE_4_CHANNEL = 876825682u,
            SPACE_5_CHANNEL = 893602898u,
            SPACE_6_CHANNEL = 910380114u,
            SPACE_7_CHANNEL = 927157330u,
            SPACE_8_CHANNEL = 943934546u,
            SPACE_9_CHANNEL = 960711762u,
            SPACE_A_CHANNEL = 1094929490u,
            SPACE_B_CHANNEL = 1111706706u,
            SPACE_C_CHANNEL = 1128483922u,
            SPACE_D_CHANNEL = 1145261138u,
            SPACE_E_CHANNEL = 1162038354u,
            SPACE_F_CHANNEL = 1178815570u,
            SPACE_sRGB = 1934772034u
        }

        private struct PROFILEHEADER
        {
            public uint phSize;
            public uint phCMMType;
            public uint phVersion;
            public uint phClass;
            public ColorSpace phDataColorSpace;
            public uint phConnectionSpace;
            public uint phDateTime_0;
            public uint phDateTime_1;
            public uint phDateTime_2;
            public uint phSignature;
            public uint phPlatform;
            public uint phProfileFlags;
            public uint phManufacturer;
            public uint phModel;
            public uint phAttributes_0;
            public uint phAttributes_1;
            public uint phRenderingIntent;
            public uint phIlluminant_0;
            public uint phIlluminant_1;
            public uint phIlluminant_2;
            public uint phCreator;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44, ArraySubType = UnmanagedType.I1)]
            public byte[] phReserved;
        }

        private struct PROFILE
        {
            public uint dwType;
            public IntPtr pProfileData;
            public uint cbDataSize;
        }
    }
}

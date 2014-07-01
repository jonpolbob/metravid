using System;
using System.Collections.Generic;

using System.Text;
using System.Runtime.InteropServices;
using DShowCap;

namespace DShowCap
{
    [ComVisible(false)]
    internal enum PinDirection
    {
        Input,
        Output
    }

    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal class AMMediaType : IDisposable
    {
        public Guid MajorType;

        public Guid SubType;

        [MarshalAs(UnmanagedType.Bool)]
        public bool FixedSizeSamples = true;

        [MarshalAs(UnmanagedType.Bool)]
        public bool TemporalCompression;

        public int SampleSize = 1;

        public Guid FormatType;

        public IntPtr unkPtr;

        public int FormatSize;

        public IntPtr FormatPtr;

        ~AMMediaType()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            // remove me from the Finalization queue 
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (FormatSize != 0)
                Marshal.FreeCoTaskMem(FormatPtr);
            if (unkPtr != IntPtr.Zero)
                Marshal.Release(unkPtr);
        }
    }

    [ComVisible(false), StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    internal class PinInfo
    {
        public IBaseFilter Filter;

        public PinDirection Direction;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Name;
    }

    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal struct VideoInfoHeader
    {
        public RECT SrcRect;

        public RECT TargetRect;

        public int BitRate;

        public int BitErrorRate;

        public long AverageTimePerFrame;

        public BitmapInfoHeader BmiHeader;
    }
    
    [ComVisible(false), StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct BitmapInfoHeader
    {
        public int Size;

        public int Width;

        public int Height;

        public short Planes;

        public short BitCount;

        public int Compression;

        public int ImageSize;

        public int XPelsPerMeter;

        public int YPelsPerMeter;

        public int ColorsUsed;

        public int ColorsImportant;
    }

    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;
    }

    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal struct CAUUID
    {
        public UInt32 cElems;
        
        public IntPtr pElems;

    }
    
    internal enum PinConnectedStatus
    {
        Unconnected = 0,
        Connected = 1,
    }

    [ComVisible(false), Flags]
    public enum SeekingCapabilities         // AM_SEEKING_SEEKING_CAPABILITIES
    {
        CanSeekAbsolute = 0x001,
        CanSeekForwards = 0x002,
        CanSeekBackwards = 0x004,
        CanGetCurrentPos = 0x008,
        CanGetStopPos = 0x010,
        CanGetDuration = 0x020,
        CanPlayBackwards = 0x040,
        CanDoSegments = 0x080,
        Source = 0x100
    }

    // Positioning and Modifier Flags
    //
    [ComVisible(false), Flags]
    public enum SeekingFlags
    {
        NoPositioning = 0x00, // No change in position
        AbsolutePositioning = 0x01, // The specified position is absolute
        RelativePositioning = 0x02, // The specified position is relative to the previous value
        IncrementalPositioning = 0x03, // The stop position is relative to the current position
        SeekToKeyFrame = 0x04, // Seek to the nearest key frame. This might be faster, but less accurate.
        ReturnTime = 0x08, // Return the equivalent reference times
        Segment = 0x10, // Use segment seeking
        NoFlush = 0x20  // Do not flush
    }


    // event notification codes
    //
    [ComVisible(false)]
    public enum EventCode
    {
        Complete = 0x01,
        UserAbort = 0x02,
        ErrorAabort = 0x03,
        Time = 0x04,
        Repaint = 0x05,
        StreamErrorStopped = 0x06,
        StreamErrorStillplaying = 0x07,

        ClockChanged = 0x0D,
        Paused = 0x0E,
        BufferingData = 0x11,

        StepComplete = 0x24
    }


    // extrait de https://code.google.com/p/msrds/source/browse/trunk/OpenCV2.0/motion_src/motion_src/dshow/Core/IMediaEvent.cs


    [ComImport,
       Guid("56A868B6-0AD4-11CE-B03A-0020AF0BA770"),
       InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IMediaEvent
    {
        // Retrieves a handle to a manual-reset event that remains
        // signaled while the queue contains event notifications
        [PreserveSig]
        int GetEventHandle(
                out IntPtr hEvent);

        // Retrieves the next event notification from the event queue
        [PreserveSig]
        int GetEvent(
                out int lEventCode,
                out int lParam1,
                out int lParam2,
                int msTimeout);

        // Waits for the filter graph to render all available data
        [PreserveSig]
        int GetEvent(
                int msTimeout,
                out int pEvCode);

        // Cancels the filter graph manager's default handling of
        // a specified event
        [PreserveSig]
        int CancelDefaultHandling(
                int lEvCode);

        // Restores the filter graph manager's default handling of
        // a specified event
        [PreserveSig]
        int RestoreDefaultHandling(
                int lEvCode);

        // Frees resources associated with the parameters of an event
        [PreserveSig]
        int FreeEventParams(
                int lEventCode,
                int lParam1,
                int lParam2);
    }



    [ComImport,
       Guid("56A868C0-0AD4-11CE-B03A-0020AF0BA770"),
       InterfaceType(ComInterfaceType.InterfaceIsDual)]
    public interface IMediaEventEx
    {
        // Retrieves a handle to a manual-reset event that remains
        // signaled while the queue contains event notifications
        [PreserveSig]
        int GetEventHandle(
                out IntPtr hEvent);

        // Retrieves the next event notification from the event queue
        [PreserveSig]
        int GetEvent(
                out int lEventCode,
                out int lParam1,
                out int lParam2,
                int msTimeout);

        // Waits for the filter graph to render all available data
        [PreserveSig]
        int WaitForCompletion(
                int msTimeout,
                out int pEvCode);

        // Cancels the filter graph manager's default handling of
        // a specified event
        [PreserveSig]
        int CancelDefaultHandling(
                int lEvCode);

        // Restores the filter graph manager's default handling of
        // a specified event
        [PreserveSig]
        int RestoreDefaultHandling(
                int lEvCode);

        // Frees resources associated with the parameters of an event
        [PreserveSig]
        int FreeEventParams(
                int lEventCode,
                int lParam1,
                int lParam2);

        // Registers a window to process event notifications
        [PreserveSig]
        int SetNotifyWindow(
                IntPtr hwnd,
                int lMsg,
                IntPtr lInstanceData);

        // Enables or disables event notifications
        // 0 - ON, 1 - OFF
        [PreserveSig]
        int SetNotifyWindow(
                int lNoNotifyFlags);

        // Determines whether event notifications are enabled
        [PreserveSig]
        int GetNotifyFlags(
                out int lNoNotifyFlags);
    }

}

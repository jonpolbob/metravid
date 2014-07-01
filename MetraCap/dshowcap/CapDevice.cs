using System;
using System.Collections.Generic;

using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows;
using LiveImage;
using DShowCap;
using System.Diagnostics;

namespace DShowCap
{
    internal class CapDevice:GenericSourceVideo,IDisposable
    {

       // [DllImport("olepro32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [DllImport("oleaut32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]          
        private static extern int OleCreatePropertyFrame(
            IntPtr hwndOwner, int x, int y,
            string lpszCaption, int cObjects,
            [In, MarshalAs(UnmanagedType.Interface)] ref object ppUnk,
            int cPages, IntPtr pPageClsID, int lcid, int dwReserved, IntPtr pvReserved);


        ManualResetEvent stopSignal;
        Thread worker;
        protected IGraphBuilder graph;
        protected ISampleGrabber grabber;
        protected IBaseFilter sourceObject, grabberObject;
        protected CapGrabber capGrabber;
        static string deviceMoniker;
        protected IMediaControl m_mediaControl = null;

        // fonction du thread
        public CapDevice()
        {
           // if (DeviceMonikes.².Count() <= 0)
             //   throw new NotSupportedException("You should connect DirectDraw source first");
            deviceMoniker = DeviceMonikes[0].MonikerString;
        }

        //--------------------------------
        // fonction du thread
        //--------------------------------
        public CapDevice(string moniker)
        {
            deviceMoniker = moniker;
        }

        //--------------------------------
        // fonction du thread
        //--------------------------------
        override public void Init(bool interactif)
        {
            CreateFilter(interactif);
            capGrabber.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(capGrabber_PropertyChanged);
            capGrabber.NewFrameArrived += new DShowCap.CapGrabber.NewFrameArrivedFunc(capGrabber_NewFrameArrived);
               
        }

        override public void Uninit()
        { 
        }
        

        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);
      
        byte fond = 0;
        int nbframesout = 0;
        long debticks = 0;
        //--------------------------------
        // newframearrived appele par le grabber
        //--------------------------------
        override protected int capGrabber_NewFrameArrived(IntPtr buffer, int buffersize)
        {

            nbframesout++;
            long deltaticks = DateTime.Now.Ticks - debticks;
            if (deltaticks > 10000000 || debticks == 0)
            {
             
                debticks = DateTime.Now.Ticks;
                Debug.WriteLine((string)"a" + nbframesout);
                nbframesout = 0;
            }
            if (m_lestack == null)
                return 0;
            CStackItem limage;

            // copie directement le buffer dans la stack et le remet dans la stack
            byte[] lebuffer = null;
            int sizex=0;
            int sizey=0;
          //  return 0;

            m_lestack.GetFreeBuffer(out limage);
            
            limage.GetBuffer(ref sizex, ref sizey, out lebuffer);
            
            if (sizex != capGrabber.Width || sizey != capGrabber.Height )
            {
                sizex = capGrabber.Width;
                sizey = capGrabber.Height;
           
                byte[] newbuffer = new byte[sizex * sizey * 4]; // a convertir en intptr
                lebuffer = newbuffer;
                 }

            int sizebuffer = sizex * sizey * 4;
            Marshal.Copy(buffer,lebuffer,  0, sizebuffer);
       
            limage.SavBuffer(sizex, sizey, lebuffer);

            m_lestack.SendFreeBuffer(limage);
            
            return 0;
        }

        override protected void capGrabber_PropertyChanged(object obj, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Width")
                Width = capGrabber.Width;

            if (e.PropertyName == "Height")
                Height = capGrabber.Height;

        }


        // fonction du thread
        void UpdateFramerate()
        {
            frames++;
            if (timer.ElapsedMilliseconds >= 1000)
            {
                Framerate = (float)Math.Round(frames * 1000 / timer.ElapsedMilliseconds);
                timer.Reset();
                timer.Start();
                frames = 0;
            }
           
        }



        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        double frames;

        // fonction du thread
        override public int Stop()
        {
            if (IsRunning)
            {
                stopSignal.Set();
                worker.Abort();
                if (worker != null)
                {
                    worker.Join();
                    Release();
                }
            }
            return 0;
        }

        // fonction du thread
        override public bool IsRunning
        {
            get
            {
                if (worker != null)
                {
                    if (worker.Join(0) == false)
                        return true;

                    Release();
                }
                return false;
            }
        }

        // fonction du thread
        void Release()
        {
            worker = null;

            stopSignal.Close();
            stopSignal = null;
        }

        // fonction du thread
        public static FilterInfo[] DeviceMonikes
        {
            get
            {
                List<FilterInfo> filters = new List<FilterInfo>();
                IMoniker[] ms = new IMoniker[1];
                ICreateDevEnum enumD = Activator.CreateInstance(Type.GetTypeFromCLSID(SystemDeviceEnum)) as ICreateDevEnum;
                IEnumMoniker moniker;
                Guid g = VideoInputDevice;
                if (enumD.CreateClassEnumerator(ref g, out moniker, 0) == 0)
                {

                    while (true)
                    {
                        int r = moniker.Next(1, ms, IntPtr.Zero);
                        if (r != 0 || ms[0] == null)
                            break;
                        filters.Add(new FilterInfo(ms[0]));
                        Marshal.ReleaseComObject(ms[0]);
                        ms[0] = null;

                    }
                }

                return filters.ToArray();
            }
        }


        virtual protected void CreateFilter(bool interactif)
        {

            capGrabber = new CapGrabber();
            graph = Activator.CreateInstance(Type.GetTypeFromCLSID(FilterGraph)) as IGraphBuilder;
                
            IVideoWindow wnd = (IVideoWindow)graph;  // masque la fenetre par defaut
            wnd.put_AutoShow(false);
            wnd = null;
                
            
                sourceObject = FilterInfo.CreateFilter(deviceMoniker);

                grabber = Activator.CreateInstance(Type.GetTypeFromCLSID(SampleGrabber)) as ISampleGrabber;
                grabberObject = grabber as IBaseFilter;
                
                graph.AddFilter(sourceObject, "source");
                graph.AddFilter(grabberObject, "grabber");

                if (interactif)
                    this.DisplayPropertyPage(2);

                using (AMMediaType mediaType = new AMMediaType())
                {
                    mediaType.MajorType = MediaTypes.Video;
                    mediaType.SubType = MediaSubTypes.RGB32;
                    grabber.SetMediaType(mediaType);

                    if (graph.Connect(sourceObject.GetPin(PinDirection.Output, 0), grabberObject.GetPin(PinDirection.Input, 0)) >= 0)
                    {
                        if (grabber.GetConnectedMediaType(mediaType) == 0)
                        {
                            VideoInfoHeader header = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.FormatPtr, typeof(VideoInfoHeader));
                           capGrabber.Width = header.BmiHeader.Width;
                           capGrabber.Height = header.BmiHeader.Height;
                           if (m_lestack != null)
                           {
                               m_lestack.Width = header.BmiHeader.Width;
                               m_lestack.Height = header.BmiHeader.Height;                           
                           }
                        }
                    }


                    graph.Render(grabberObject.GetPin(PinDirection.Output, 0));
                    grabber.SetBufferSamples(false);
                    grabber.SetOneShot(false);
                    grabber.SetCallback(capGrabber, 1);

                    //IVideoWindow wnd = (IVideoWindow)graph;
                    //wnd.put_AutoShow(false);
                    //wnd = null;
                }
        }

        // fonction du thread
       /* void RunWorker()
        {
            try
            {

                    IMediaControl control = (IMediaControl)graph;
                    control.Run();

                    while (!stopSignal.WaitOne(0, true))
                    {
                        Thread.Sleep(10);
                    }

                    control.StopWhenReady();            
            }

            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                graph = null;
                sourceObject = null;
                grabberObject = null;
                grabber = null;
                capGrabber = null;
                IMediaControl control = null;
                
            }
            
        }*/



        //--------------------
        // demarrage du filtre
        //--------------------
        override public int Start()
        {
            IMediaControl control = (IMediaControl)graph;
            control.Run();
            return 0;
 
        }


        IPin findpinbydirection(IBaseFilter source, PinDirection srchdirection, int numpin)
        {IEnumPins enumpins;
         source.EnumPins(out enumpins);
         IPin[] pintab = new IPin[1];
         bool Encore  = true;
         int curpin =0;
         while (Encore)
         {
             int fetched = 0;
             enumpins.Next(1, pintab, out fetched);
             PinDirection direction;   
             pintab[0].QueryDirection(out direction);
             if (direction == srchdirection)
                { if (curpin++ == numpin)
                 return pintab[0];
                }
            }
           return null;
        }

        override public void DisplayPropertyPage(int typpage)
        {
            DisplayPropertyPage(sourceObject, typpage);
        }
        

        private void DisplayPropertyPage(IBaseFilter dev, /*Control Parent,*/ int typepage)
        {
            //Get the ISpecifyPropertyPages for the filter
            ISpecifyPropertyPages pProp = dev as ISpecifyPropertyPages;
            int hr = 0;
            IPin iPinOutSource = null;

            if (pProp == null)
                return;

            //Get the name of the filter from the FilterInfo struct
            //FilterInfo filterInfo=new FilterInfo(("");
            //hr = dev.QueryFilterInfo(filterInfo);


            // Get the propertypages from the property bag
            CAUUID caGUID = new CAUUID();
            caGUID.cElems = 0;
            caGUID.pElems = IntPtr.Zero;

            hr = pProp.GetPages(out caGUID);
                //.GetPages(out caGUID);
            
            if (hr != 0)
                throw new DeviceCreateException(1, "getproppages");

            // Check for property pages on the output pin
            ISpecifyPropertyPages pinProp = null;
            if (sourceObject != null)
            {
                //iPinOutSource = sourceObject.DsFindPin.ByDirection(sourceObject, PinDirection.Output, 0);
                iPinOutSource = findpinbydirection(sourceObject, PinDirection.Output, 0);
                pinProp = iPinOutSource as ISpecifyPropertyPages;
            }
            CAUUID caGUID2 = new CAUUID();

            if (pinProp != null)
            {
                hr = pinProp.GetPages(out caGUID2);
                if (hr != 0)
                    throw new DeviceCreateException(1, "getproppages 2");
                //DsError.ThrowExceptionForHR(hr);

                if (caGUID.cElems > 0)
                {
                    int soGuid = Marshal.SizeOf(typeof(Guid));

                    // Create a new buffer to hold all the GUIDs
                    IntPtr p1 = Marshal.AllocCoTaskMem((int)(caGUID.cElems) * soGuid);

                    // Copy over the pages from the Filter
                    for (int x = 0; x < caGUID.cElems * soGuid; x++)
                    {
                        Marshal.WriteByte(p1, x, Marshal.ReadByte(caGUID.pElems, x));
                    }

                    // Release the old memory
                    Marshal.FreeCoTaskMem(caGUID.pElems);

                    // Reset caGUID to include both
                    caGUID.pElems = p1;
                }


                if (caGUID2.cElems > 0)
                {
                    int soGuid2 = Marshal.SizeOf(typeof(Guid));

                    // Create a new buffer to hold all the GUIDs
                    IntPtr p2 = Marshal.AllocCoTaskMem((int)(caGUID.cElems) * soGuid2);

                    // Copy over the pages from the Filter
                    for (int x = 0; x < caGUID.cElems * soGuid2; x++)
                    {
                        Marshal.WriteByte(p2, x, Marshal.ReadByte(caGUID2.pElems, x));
                    }

                    // Release the old memory
                    Marshal.FreeCoTaskMem(caGUID2.pElems);

                    // Reset caGUID to include both
                    caGUID2.pElems = p2;
                }
            }

            // Create and display the OlePropertyFrame
            // ca amrche mais les controles sont grises. il faudrait defaire le lien ?

            // essai de deconnection des pins)
            m_mediaControl = (IMediaControl)graph;

            object oDevice = (object)dev;
            object oPin = (object)iPinOutSource;
             if (typepage == 1)
                hr = OleCreatePropertyFrame(IntPtr.Zero/*Parent.Handle*/, 0, 0, "toto", 1, ref oDevice, (int)caGUID.cElems, caGUID.pElems, 0, 0, IntPtr.Zero);

            if (typepage == 2)
                hr = OleCreatePropertyFrame(IntPtr.Zero, 0, 0, "titi", 1, ref oPin, (int)caGUID2.cElems, caGUID2.pElems, 0, 0, IntPtr.Zero);
       

            //DsError.ThrowExceptionForHR(hr);
            if (hr != 0)
                throw new DeviceCreateException(1, "olecratepropertyframes");


            // Release COM objects
            Marshal.FreeCoTaskMem(caGUID.pElems);
            Marshal.FreeCoTaskMem(caGUID2.pElems);
      //      Marshal.ReleaseComObject(pProp);
        
        }




        static readonly Guid FilterGraph = new Guid(0xE436EBB3, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

        static readonly Guid SampleGrabber = new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);

        public static readonly Guid SystemDeviceEnum = new Guid(0x62BE5D10, 0x60EB, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

        public static readonly Guid VideoInputDevice = new Guid(0x860BB310, 0x5D01, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);

        [ComVisible(false)]
        internal class MediaTypes
        {
            public static readonly Guid Video = new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Interleaved = new Guid(0x73766169, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Audio = new Guid(0x73647561, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Text = new Guid(0x73747874, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid Stream = new Guid(0xE436EB83, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);
        }

        [ComVisible(false)]
        internal class MediaSubTypes
        {
            public static readonly Guid YUYV = new Guid(0x56595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid IYUV = new Guid(0x56555949, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid DVSD = new Guid(0x44535644, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

            public static readonly Guid RGB1 = new Guid(0xE436EB78, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB4 = new Guid(0xE436EB79, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB8 = new Guid(0xE436EB7A, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB565 = new Guid(0xE436EB7B, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB555 = new Guid(0xE436EB7C, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB24 = new Guid(0xE436Eb7D, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid RGB32 = new Guid(0xE436EB7E, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid Avi = new Guid(0xE436EB88, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

            public static readonly Guid Asf = new Guid(0x3DB80F90, 0x9412, 0x11D1, 0xAD, 0xED, 0x00, 0x00, 0xF8, 0x75, 0x4B, 0x99);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        public event EventHandler OnNewBitmapReady;


       }
}

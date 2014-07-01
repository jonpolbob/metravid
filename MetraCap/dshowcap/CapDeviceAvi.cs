using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Diagnostics;

namespace DShowCap
{
    class CapDeviceAvi : CapDevice
    {
        private const int WM_APP = 0x8000;
        private const int WM_GRAPHNOTIFY = WM_APP + 1;
        private const int EC_COMPLETE = 0x01; 

    

        public CapDeviceAvi()           
        { }

        public override bool CanOpenFile() // il existe un menu d'ouverture
        {
            return true;
        }

        IBaseFilter inputfilter =null;

        public override bool SetFilename(string nomfile)
        {
            // ici on stoppe l'ancien fichier
            if (inputfilter != null)
                graph.RemoveFilter(inputfilter);


            graph.AddSourceFilter(nomfile, "Source", out inputfilter);
   

            using (AMMediaType mediaType = new AMMediaType())
                {
                    mediaType.MajorType = MediaTypes.Video;
                    mediaType.SubType = MediaSubTypes.RGB32;
                    grabber.SetMediaType(mediaType);

                    if (graph.Connect(inputfilter.GetPin(PinDirection.Output, 0), grabberObject.GetPin(PinDirection.Input, 0)) >= 0)
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
                }

               graph.Render(grabberObject.GetPin(PinDirection.Output, 0));
      
               IVideoWindow wnd = (IVideoWindow)graph;  // masque la fenetre par defaut : a mettre apres le render
               wnd.put_AutoShow(false);
               wnd = null;
      
        //       grabber.SetBufferSamples(false);
        //       grabber.SetOneShot(false);
        //       grabber.SetCallback(capGrabber, 1);

               
      
            return true;
        }



        // reinit le graph avec les caracteristiques du nouveau fichier
        void setnewfile()
        {
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
            }
        }

        //------------------------------
        // pas de menu de config
        //------------------------------
        override public bool CanConfigure() // il exiqste un menu de configuration
        {
            return false;
        }

        //-------------------
        // creation du filtre
        //-------------------        
        override protected void CreateFilter(bool interactif)
        {
            capGrabber = new CapGrabber();
            graph = Activator.CreateInstance(Type.GetTypeFromCLSID(FilterGraph)) as IGraphBuilder;

            IMediaSeeking ISeek = graph as IMediaSeeking;
            ISeek.SetRate(60);


            grabber = Activator.CreateInstance(Type.GetTypeFromCLSID(SampleGrabber)) as ISampleGrabber;
            grabberObject = grabber as IBaseFilter;

            graph.AddFilter(grabberObject, "grabber");

            grabber.SetBufferSamples(false);
            grabber.SetOneShot(false);
            grabber.SetCallback(capGrabber, 1);


            IMediaEventEx objMediaEventEx = graph as IMediaEventEx;
            Window wnd = Application.Current.MainWindow;
            IntPtr hwnd = new WindowInteropHelper(wnd).Handle;
            objMediaEventEx.SetNotifyWindow(hwnd,WM_GRAPHNOTIFY,IntPtr.Zero); 
      
        }


        // process du message completed pour revenir au debut si on loop

         override public void  ProcessSysMessage(Int16 WParam, Int32 lParam, ref bool handled)
        {
            IMediaEvent objMediaEvent = graph as IMediaEvent;
            int lEventCode;
            int Param1;
            int Param2;
            int TimeOut =0;

            while (objMediaEvent.GetEvent(out lEventCode, out Param1, out Param2, 0) == 0)
            {
                Debug.WriteLine("event");

                if (lEventCode == (int)EventCode.Complete)
                { Debug.WriteLine("complete");
                Debug.WriteLine("encore");
                IMediaSeeking objMediaSeek = graph as IMediaSeeking;
                long Current =0;
                long End = 0;

                objMediaSeek.SetPositions(ref Current, SeekingFlags.AbsolutePositioning, ref End, SeekingFlags.NoPositioning);
            
                }
            }


         
        }
                

        static readonly Guid FilterGraph = new Guid(0xE436EBB3, 0x524F, 0x11CE, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70);

        static readonly Guid SampleGrabber = new Guid(0xC1F400A0, 0x3F08, 0x11D3, 0x9F, 0x0B, 0x00, 0x60, 0x08, 0x03, 0x9E, 0x37);

    }

    
}

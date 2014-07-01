using System;
using System.Collections.Generic;

using System.Text;
using System.Windows.Interop;
using System.Windows;

using System.Threading;
using System.ComponentModel;
using DShowCap;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace LiveImage
{
    public class CapStack : IDisposable
    {
        // plans a utiliser pour l'image

        // thread de lecture des images
        ManualResetEvent stopSignal;
        Thread worker;

        List<CStackItem> m_readyimages = new List<CStackItem>();
        List<CStackItem> m_freeimages = new List<CStackItem>();


        public delegate void ShowImageDelegate(int sizex, int sizey, byte[] Buffer, int sizebuffer);
        public ShowImageDelegate CallShowImage = null;
        //public PropertyChangedEventHandler CallShowImage = null;
       

        int m_Height = default(int);
        int m_Width = default(int);

        GenericSourceVideo ledevice = null;


        public int DisplayPropertyPage(int typpage)
        {
         ledevice.DisplayPropertyPage(typpage);
         return 0;
        }


        public int Init(bool interactif)
        {
            ledevice = new CapDeviceAvi();
            //ledevice = new CapDevice(); 

            ledevice.SetCapStack(this);
            ledevice.Init(interactif);
           // ledevice.DisplayPropertyPage(2); 
            
            m_Height = ledevice.Height;
            m_Width = ledevice.Width;
            //ledevice.SetFilename(@"L:\stockage\avi\daphnia.avi");
            ledevice.SetFilename(@"E:\stockage\video\medaka 6dpf 1.avi");
            return 0;
        }
      
        public int Start()
        {
            if (ledevice != null)
                ledevice.Start();

            return 0;
        }


        // appele par le hook de procwnd pour le traitement des messages
        public void ProcessSysMessage(IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            Int16[] WParam = new Int16[1];
            Int32[] LParam = new Int32[1];
            WParam[0] = 0;
            LParam[0] = 0;
            if (wParam != IntPtr.Zero)
                Marshal.Copy(wParam, WParam, 0, 1);
            if (lParam != IntPtr.Zero)
                Marshal.Copy(lParam, LParam, 0, 1);
            
            //if (WParam[0] == (Int16)EventCode.Complete)
            {
                ledevice.ProcessSysMessage(WParam[0], LParam[0], ref handled);
              //  Debug.WriteLine("ec-complete");
               // handled = true;
                }

            
        }



        public void Uninit()
        {
            Encore = false;
            stopSignal.Set();
    
        }
        
        public PropertyChangedEventHandler PropertyChanged = null;

        private void OnPropertyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }


        // width height contiennent le width et le height de l'image de la stack. 
        // si ca change : on vide la stack
        public int Width
        {
            get { return m_Width; }
            set
            {
                m_Width = value;
                ClearReadyStack();// on vide la liste des images car elle n'a plus la bonne taille
                OnPropertyChanged("Width");
            }
        }


        

        public int Height
        {
            get { return m_Height; }
            set
            {
                m_Height = value;
                ClearReadyStack(); // on vide la liste des images car elle n'a plus la bonne taille
                OnPropertyChanged("Height");
            }
        }


        
        //----------------------------------------
        // vide la pile des ready dans les free
        //----------------------------------------
        void ClearReadyStack()
        {foreach (CStackItem i in m_readyimages)
            {m_freeimages.Add(i);
             m_readyimages.Remove(i);
            }
        }

        
        public CapStack()
        {// on init un peu la pile des images
         m_freeimages.Add(new CStackItem());
         m_freeimages.Add(new CStackItem());
         m_freeimages.Add(new CStackItem());
         m_freeimages.Add(new CStackItem());
           
         mySendImage = new SendImageDelegate(sendimageproc);

         stopSignal = new ManualResetEvent(false);
         stopSignal.Reset(); 
         worker = new Thread(RunWorker);
         worker.Start();

     //   ledevice.SetCapStack(this);
     //    ledevice.Init();
     //    ledevice.Start();
        }




       
        
        //------------------------------------------
        // recupere un buffer dans les bufers libres
        //------------------------------------------
        public int GetFreeBuffer(out CStackItem img)
        {
            img = null;

            if (m_freeimages.Count != 0)
            {
                img = m_freeimages[m_freeimages.Count - 1];
                return 0;
            }

            return 1;
        }

        bool Encore = true;

        //---------------------------
        // range le buffer venant de la cam dans les buffer a traiter
        //---------------------------
        public int SendFreeBuffer(CStackItem img)
        {
         int x=0;
         int y=0;
         byte[] buffer;
         img.GetBuffer(ref x, ref y, out buffer);
         
         // la dimension a change : on resette les dimension du systeme

         if (x != Width)
            Width = x;

         if (y != Height)
            Height = y;

         m_readyimages.Add(img);

         stopSignal.Set(); // lance l'afficahge
         
         return 0;
        }


        public delegate void SendImageDelegate(CStackItem st,int sizex, int sizey, byte[] Buffer, int nbpix);
        public SendImageDelegate mySendImage;

        void sendimageproc(CStackItem st, int sizex, int sizey,byte[] Buffer,int sizebuff)
                {//UI thread does post update operations
                  SendImage(sizex, sizey, Buffer, sizebuff); // on affiche cette image
                     m_freeimages.Add(st);  // on la bascule dans les free
                     m_readyimages.Remove(st);
                 }
                 
        //-----------------------------------------------------------------------------------
        // thread lisant la prochaine image de la pile et l'envoie au composant d'afficahge
        // pour copie dans sa map
        // ensuite le buffer st mis dans les free
        //-----------------------------------------------------------------------------------
        byte outval = 0;
        byte nbframesout = 0;
        long debticks = 0;
        //----------------------------------------------
        // fonction du thread de lecture de la pile
        //----------------------------------------------
        void RunWorker()
        {
            try
            {
               while (Encore)
               if (stopSignal.WaitOne(0, true)) // attent d'un evenement
               {
                   stopSignal.Reset();
                while (m_readyimages.Count > 1) // vidange des images en trop
                    {
                    CStackItem st = m_readyimages[0];
                    m_freeimages.Add(st);  // on la bascule dans les free
                    m_readyimages.Remove(st);                     
                   }

                // derniere image : on l'affiche
                if (m_readyimages.Count != 0)  // on hcherche dans la pile qui est utilisable
                   {CStackItem st = m_readyimages[0]; 
                    byte[] Buffer;
                    int sizex=0;
                    int sizey=0;

                    st.GetBuffer(ref sizex, ref sizey, out Buffer);
                    int sizebuff = sizex * sizey * 4; // 32bpp

               /*     for (int i = 0; i < sizebuff; i++)
                        Buffer[i] = outval;
                    if (outval > 130)
                        outval = 0;
                    else
                        outval += 10;
                    */
                    if (Application.Current != null)
                    if (Application.Current.Dispatcher != null)
                        Application.Current.Dispatcher.Invoke(mySendImage,new Object[] {st,sizex, sizey, Buffer, sizebuff});

                    nbframesout++;
                    long deltaticks = DateTime.Now.Ticks - debticks;
                    if (deltaticks > 10000000 || debticks==0)
                    {
                        debticks = DateTime.Now.Ticks;
                        Debug.WriteLine((string)"v"+nbframesout);
                        nbframesout = 0;
                        }
                    Thread.Sleep(10);
                 
               }
               
                    
           
               }
            }
           catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                
            }

        }



      

        // envoie l'image au coposant de visualissation pour copie 
        // via le delegate mis en place par le composant e visualisation
        public void SendImage(int x, int y, byte[] buffer, int sizebuff)
        {
            if (CallShowImage != null)
                CallShowImage(x, y, buffer, sizebuff);
        }






        #region IDisposable Members

        public void Dispose()
        {Encore = false;
        stopSignal.Set();
         //   throw new NotImplementedException();
        }

        #endregion
    }
}

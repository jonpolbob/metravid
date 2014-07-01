using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using System.Threading;

namespace LiveImage
{
    public class LiveImage : Image, IDisposable
    {
        WriteableBitmap LaBitmap = null;// new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);

        // imports
        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);


        //-------------------------------------------
        int BuffWidth = default(int);
        int BuffHeight = default(int);
        IntPtr Map=IntPtr.Zero;  // buffer utilise pour la bitmap
        IntPtr section = IntPtr.Zero;


        // constructeur
        public LiveImage()
        {
            BuffWidth = 640;
            BuffHeight = 480;
            
            uint pcount = (uint)(BuffWidth * BuffHeight * PixelFormats.Bgr32.BitsPerPixel / 8);
            LaBitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);

            /*
            section = CreateFileMapping(new IntPtr(-1), IntPtr.Zero, 0x04, 0, pcount, null);
            Map = MapViewOfFile(section, 0xF001F, 0, 0, pcount);
            BitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromMemorySection(section, BuffWidth, BuffHeight, PixelFormats.Bgr32,
                BuffWidth * PixelFormats.Bgr32.BitsPerPixel / 8, 0) as InteropBitmap;
            
            this.Source = BitmapSource; // ca c'est la Source de Image                    
            
             */
            this.Source = LaBitmap;
            
            byte[] nbuffer =new byte[pcount];
            int curpos =0;
            for (int ligne=0;ligne<BuffHeight;ligne++)
                for (int col=0;col<16;col++)
                    {for (int Pix=0; Pix<(BuffWidth/16);Pix++)
                    {nbuffer[curpos++]=(byte)(col*16);
                     nbuffer[curpos++]=(byte)(col*16);
                     nbuffer[curpos++] = (byte)(col * 16);
                     nbuffer[curpos++]=128;
                    }
                    
             Stack_UpdateImage(BuffWidth, BuffHeight, nbuffer, (int)pcount);
       

             }

            Application.Current.Exit += new ExitEventHandler(Current_Exit);
        }

        // on sortie
        void Current_Exit(object sender, ExitEventArgs e)
        {
            this.Dispose();
        }


        InteropBitmap BitmapSource;

        //--------------------------------------------------------
        //gestion de la bitmapsource qui sera utilisee par la fenetre
        //--------------------------------------------------------
        /*public InteropBitmap BitmapSource
        {
            get { return (InteropBitmap)GetValue(BitmapSourceProperty); }
            private set { SetValue(BitmapSourcePropertyKey, value); }
        }*/

        //---- pour pouvoir utiliserl'image comme composant
        /*private static readonly DependencyPropertyKey BitmapSourcePropertyKey =
            DependencyProperty.RegisterReadOnly("BitmapSource", typeof(InteropBitmap), typeof(LiveImage), new UIPropertyMetadata(default(InteropBitmap)));
        
        public static readonly DependencyProperty BitmapSourceProperty = BitmapSourcePropertyKey.DependencyProperty;
        */

        //-------------------------------------------------
        // gestion de la connection avec le stack
        //-------------------------------------------------
        CapStack m_lestack = null; // a quel stack se truc est acccroche

        //----------------------------------------------------
        // fonction positionnant le stack pour qu'elle soit sensible a une entree
        //----------------------------------------------------
        public void setstack(CapStack LeStack)
        {
         if (m_lestack != null)
         {
             LeStack.CallShowImage = null;
             LeStack.PropertyChanged = null;
            }


         m_lestack = LeStack;
        
         if (LeStack != null)
            {
            LeStack.CallShowImage += new CapStack.ShowImageDelegate(Stack_UpdateImage);
            LeStack.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Stack_PropertyChanged);

            // simeule propertychanged pour initialiser la map
            Stack_PropertyChanged(null, new System.ComponentModel.PropertyChangedEventArgs("Width")); // met a jour les dimensions acvec celle du stack
            }
        }

        byte fond = 0;
        //-----------------------------------------
        // mise a jour de l'image
        // appele depuis le thread de lecture de la stack
        // est appelle depuis le thread de lecture de la stack
        // apres etre passe comme delegate depuis le stack
        // pour mettre a jour l'image a partir de ce qu'il y a dans la stack
        //-----------------------------------------
        void Stack_UpdateImage(int sizex, int sizey, byte[] buffer, int size)
        {
            if (BuffWidth != sizex || BuffHeight != sizey)
            { BuffWidth = sizex;
             BuffHeight = sizey;
             return; // affichage impossible les dimensions sont pas bonnes
            } 
                           
         //   if (Map == IntPtr.Zero)
   //             return;


          /*  for (int i = 0; i < size; i++)
                buffer[i] = fond;
            fond += 10;
            if (fond == 128)
                fond = 0;
            */

          
          //NativeMethods.CopyMemory(this.writeableBitmap.BackBuffer, data.Scan0, ImageBufferSize);
          LaBitmap.Lock();
          IntPtr dest = LaBitmap.BackBuffer;
          Marshal.Copy(buffer, 0, dest, size);
          LaBitmap.AddDirtyRect(new Int32Rect(0, 0, BuffWidth, BuffHeight));
          LaBitmap.Unlock();
          
         
          // copier buffer dans la map et appleler invalidate
         /* if (this.Dispatcher != null)
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, (SendOrPostCallback)delegate
                {
                    if (BitmapSource != null)
                    {
                        BitmapSource.Invalidate();
                       // UpdateFramerate();
                    }
                }, null);
            }
          */
        }

        public void DisplayPropertyPage(int typpage)
        {
        m_lestack.DisplayPropertyPage(typpage);
        }
        
        //-------------------------------------------
        // changement de taille des images de la pile : on change la taille de la map
        //-------------------------------------------
        void Stack_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Width")
                BuffWidth = m_lestack.Width;
            
            if (e.PropertyName == "Height")
                BuffHeight = m_lestack.Height;
            

            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.DataBind, (SendOrPostCallback)delegate
            {
                if (BuffWidth != default(int) && BuffHeight != default(int))
                {

                   
                    uint pcount = (uint)(BuffWidth * BuffHeight * PixelFormats.Bgr32.BitsPerPixel / 8);
                    LaBitmap = new WriteableBitmap(BuffWidth, BuffHeight, 96, 96, PixelFormats.Bgr32, null);

                  
                    this.Source = LaBitmap; // ca c'est la Source de Image                    
                }
            }, null);
        
        }


        #region IDisposable Members

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        #endregion
    }
}

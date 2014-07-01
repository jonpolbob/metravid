using System;
using System.Collections.Generic;

using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.ComponentModel;

namespace DShowCap
{
    internal class CapGrabber:ISampleGrabberCB,INotifyPropertyChanged
    {
        public CapGrabber()
        {
            
        }

        public int Width
        {
            get { return m_Width; }
            set
            {
                m_Width = value;
                OnPropertyChanged("Width");
            }
        }

        public int Height
        {
            get { return m_Height; }
            set
            {
                m_Height = value;
                OnPropertyChanged("Height");
            }
        }

        int m_Height = default(int);

        int m_Width = default(int);


        //---------------------------------------------------------
        //---------------------------------------------------------
        #region ISampleGrabberCB Members
        public int SampleCB(double sampleTime, IntPtr sample)
        {
            return 0;
        }


        public delegate int NewFrameArrivedFunc(IntPtr buffer, int buffersize);

        public NewFrameArrivedFunc NewFrameArrived;

        //---------------------------------------------------------
        //---------------------------------------------------------
        public int BufferCB(double sampleTime, IntPtr buffer, int bufferLen)
        {// cette fonction appelle la delegate qui mettra ca dans le stack
        
           // dans l'ideal il faudrait verifier que la taille est bonne
         if (NewFrameArrived != null)
            NewFrameArrived(buffer, bufferLen);
        
         return 0;
        }

        #endregion

        

        
        
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;

namespace LiveImage
{
    abstract class GenericSourceVideo : DependencyObject
    {

        protected CapStack m_lestack = null;

        //--------------------------------
        // dit a quelle capstack envoyer
        //--------------------------------
        public void SetCapStack(CapStack lestack)
        {
            m_lestack = lestack;
            m_lestack.Width = Width;
            m_lestack.Height = Height;
        }

        int m_Width=0;
        int m_Height=0;

        public int Width
        {
            get{return m_Width;}
            set{
                m_Width = value;
                if (m_lestack != null)
                    m_lestack.Width = value;
                }

        }
        public int Height
        {
            get{return m_Height;}
            set{
                m_Height = value;
                if (m_lestack != null)
                    m_lestack.Height= value;
                }
        }

        // propage le changement de taille de l'aimage au stack
        abstract protected void capGrabber_PropertyChanged(object obj, System.ComponentModel.PropertyChangedEventArgs e);

        virtual public void  ProcessSysMessage(Int16 WParam, Int32 lParam, ref bool handled)
        { handled = false;
        }
        //-------------------------------------------------------
        // pour avoir le framerate en property pour le composant
        //-------------------------------------------------------
        public float Framerate
        {
            get { return (float)GetValue(FramerateProperty); }
            set { SetValue(FramerateProperty, value); }
        }

        public static readonly DependencyProperty FramerateProperty =
            DependencyProperty.Register("Framerate", typeof(float), typeof(GenericSourceVideo), new UIPropertyMetadata(default(float)));

        
        //-------------------
        // a surcharger
        //-------------------
        abstract public void Init(bool interactif);
        abstract public void Uninit();

        abstract public void DisplayPropertyPage(int typpage);

        abstract public int Stop();
        abstract public int Start();

        virtual public bool CanOpenFile() // il existe un menu d'ouverture
        { return false; 
        }

        virtual public bool SetFilename(string nomfile) // programme un nom (de fichier) e tprepare la lecture
        {
            return false;
        }

        virtual public bool CanConfigure() // il exiqste un menu de configuration
        { return false; 
        }

        virtual public bool CanFreeze() // il existe un menu de freze/run
        { return false; 
        }

        virtual public bool CanNavigate() // il existe des fonction pour se deplacer dans le flux video
        { return false; 
        }

        abstract protected int capGrabber_NewFrameArrived(IntPtr buffer, int buffersize);

        virtual public bool IsRunning
        {
            get;
            set;
        }
    
        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion
  

    }
}

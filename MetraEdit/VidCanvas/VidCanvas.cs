using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;




namespace MetraEdit
{
    public class VidCanvas : Canvas
    {
        Point m_Pointrefmove;
        int m_curtypfig = 1;
        Cursor m_CurrentCursor = Cursors.Arrow;
        List<UndoItem> m_undolist = new List<UndoItem>();
        VidPath m_CurrentItem;
        private List<VidPath> m_paths = new List<VidPath>();

        // objet en cours de modif
        List<VidPath> m_selected = new List<VidPath>();
        int m_CurrentItemNum = 0;
        int m_numhandle = 0;

        public enum VisModeEdit
        {
            selectitem = 0,
            selecthandle = 1,
            create = 2,
            movehandle = 3,
            movhandlecreate = 4,
            selecthandleitem = 5,
            moveitem=6
        }

        public int HandleSensDistance
        {
            get;
            set;
        }

        public int HandleSize
        {
            get;
            set;
        }

        public int LineSensDistance
        {
            get;
            set;
        }

        public VidCanvas()
        {
            BorderColor = Brushes.Red;
            HandleSensDistance = 5;
            LineSensDistance = 3;
            HandleSize = 3;
        }

        //------------------------------
        // cree une nouvelle figure
        //------------------------------
        VidPath BuildFigure(Point pt)
        {
            Int64 ID = IDGenerator.GetID();

            switch (m_curtypfig)
            {
                case 1:
                    return new VidRect(this, ID , pt);

                default:
                    break;
            }

            // metrte un throw ?
            return null;
        }


        //----------------------------------------------
        //
        //----------------------------------------------
        public void DeleteItem(VidPath lepath)
        {
            Children.Remove(lepath.m_lepath);
            m_paths.Remove(lepath);
        }

        public void unselectall()
        {
            foreach (VidPath v in this.m_paths)
                v.SetSelected(-1, false); // desel tout

        }

        //----------------------------------------------
        //
        //----------------------------------------------
        public int AddUndo(Int64 ID,string action)
        {
            m_undolist.Add(new UndoItem(ID,action));
            return 0;
        }

        //----------------------------------------------
        //
        //----------------------------------------------
        public int Undo()
        {
            if (m_undolist.Count == 0)
                return 0;
            
            bool Encore=true;

            while (Encore)
            {
                Encore = false;

                foreach (VidPath v in m_paths)
                    if (v.IsID(m_undolist[m_undolist.Count - 1].ID))
                    {
                        if (!m_undolist[m_undolist.Count - 1].todelete)
                        {
                            v.UndoExec(m_undolist[m_undolist.Count - 1].action);
                            {
                                m_undolist[m_undolist.Count - 1].todelete = true;
                                Encore = true;
                            }
                        }
                     break;  // on reboucle car la liste m_paths risque d'avoir change
                    }
                
            }

            // on vidange la liste des undo traites
            Debug.Print(" effacement");
            m_undolist.RemoveAll(x => ((UndoItem)x).todelete);
            
            return 1;
        }


        /// <summary>
        /// ajoute un nouveau path a la liste 
        /// </summary>
        /// <param name="newpath"></param>
        /// <returns></returns>
        public int AddPath(VidPath newpath)
        {
            this.m_paths.Add(newpath);
            Children.Add(m_paths[m_paths.Count - 1].m_lepath);
            return m_paths.Count - 1;
        }

        /// <summary>
        /// 
        /// </summary>
        public VisModeEdit ModeEdit
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int ModeCreate
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>     
        public Brush FillColor
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Brush BorderColor
        {
            get;
            set;
        }


        void deseltout()
        {foreach(VidPath v in m_paths)
            v.SetSelected(-1,false);
        }
        
        /// <summary>
        /// clic en mode selection
        /// </summary>
        /// <param name="PointPos"></param>
        /// <returns></returns>
        private VisModeEdit MouseDownSelectHandle(Point PointPos)
        {
            VidPath leItem = null;
            int handle = CanvasHandleSelect((int)PointPos.X, (int)PointPos.Y, ref leItem);
            m_numhandle = handle;
            m_Pointrefmove = PointPos; // point reference position souris
            deseltout();
            if (handle != -1)
                {
                 m_CurrentItem = leItem;           
                 m_CurrentItemNum = m_paths.IndexOf(leItem);
                 leItem.StartChange();
                 leItem.SetSelected(handle, true);
                 if (handle ==0)
                     return VisModeEdit.moveitem;
                 else
                     return VisModeEdit.movehandle;
                }
            else
                return VisModeEdit.selectitem;
        }



        /// <summary>
        /// clique en mode premier point creation 
        /// </summary>
        /// <param name="PointPos"></param>
        /// <returns></returns>
        private VisModeEdit MouseDownCreate(Point PointPos)
        {
            VidPath newfig = BuildFigure(PointPos);
            int NumRect = AddPath(newfig);
            newfig.AddUndo("create"); // undo de la creation
            
            m_CurrentItem = newfig;
            m_numhandle = newfig.GetNxtHandleCreate(0);
            m_Pointrefmove = PointPos; // point reference position souris
            m_CurrentItem.StartChange(false); // stockage coocrdonnees debut creation (mais pas de undo)

            return VisModeEdit.movhandlecreate; // mode deplacemetn andle en creation
        }

        // click souris sur fin deplacement handle en cours creation : on passe au pt suivant
        private VisModeEdit MouseDownHdlCreate(Point PointPos)
        {
            m_Pointrefmove = PointPos; // point reference position souris
            m_numhandle = m_CurrentItem.GetNxtHandleCreate(m_numhandle);
            if (m_numhandle == 0)
                return VisModeEdit.selecthandle; // revient en mode selection
            else
                return VisModeEdit.movhandlecreate;  // inchangé
        }

        
        /// <summary>
        /// click fin de deplacement poignee creation 
        /// </summary>
        /// <param name="PointPos"></param>
        /// <returns></returns>
        private VisModeEdit MouseDownHdlMove(Point PointPos)
        {

            return VisModeEdit.selecthandle; // revient en mode selection
        }


        /// <summary>
        /// click fin de deplacement item
        /// </summary>
        /// <param name="PointPos"></param>
        /// <returns></returns>
        private VisModeEdit MouseDownItemMove(Point PointPos)
        {

            return VisModeEdit.selecthandle; // revient en mode selection
        }


        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PointPos"></param>
        public void VMouseDown(Point PointPos)
        {
            switch (ModeEdit)
            {
                case VisModeEdit.selecthandle:

                case VisModeEdit.selecthandleitem:
                case VisModeEdit.selectitem:  // clic select dans un des modes select
                    ModeEdit = MouseDownSelectHandle(PointPos);
                    break;

                case VisModeEdit.create: // clic pose pt creation
                    ModeEdit = MouseDownCreate(PointPos);
                    break;

                case VisModeEdit.movehandle:  // clic fin mov handle
                    ModeEdit = MouseDownHdlMove(PointPos);
                    break;

                case VisModeEdit.moveitem:  // clic fin move item
                    ModeEdit = MouseDownItemMove(PointPos);
                    break;


                case VisModeEdit.movhandlecreate:
                    ModeEdit = MouseDownHdlCreate(PointPos);
                    break;
            }
        }


        /// <summary>
        /// utilistaire de changement curseur en cas de boesoin
        /// </summary>
        /// <param name="newcursor"></param>
        /// <returns></returns>
        private Cursor VSetCursor(Cursor newcursor)
        {
            Cursor oldcursor = Cursor;

            if (Cursor != newcursor)
                Cursor = newcursor;

            return oldcursor;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="Position"></param>        
        public void VMouseMove(Point Position)
        {
            Point newpos = m_Pointrefmove;
            Debug.WriteLine("ptref" + m_Pointrefmove.ToString());
            newpos.Offset(-Position.X, -Position.Y);


            VidPath VidPath = null;

            switch (ModeEdit)
            {
                case VisModeEdit.selectitem:
                case VisModeEdit.selecthandle:
                    {
                        int tocursor = CanvasHandleSelect((int)Position.X, (int)Position.Y, ref VidPath);

                        // changement de curseur selon la ou on se trouve
                        switch (tocursor)
                        {
                            case -1:
                                VSetCursor(m_CurrentCursor);
                                break;

                            case 0:
                                VSetCursor(Cursors.SizeAll);
                                break;

                            default:
                                VSetCursor(Cursors.Hand);
                                break;
                        }
                    }
                    break;


                // mode movehandle = on eplace la poignee courante
                case VisModeEdit.movhandlecreate:
                case VisModeEdit.movehandle:                
                    VSetCursor(Cursors.Pen);                        
                    m_CurrentItem.MovePoignee(m_numhandle, (int)newpos.X, (int)newpos.Y);
                    break;

                case VisModeEdit.moveitem:
                    VSetCursor(Cursors.Hand);
                    m_CurrentItem.MoveLine((int)newpos.X, (int)newpos.Y);
                    break;
            }
        }


        /// <summary>
        /// renvoie -1 si rien trouve, 0 si ligne et autre numero si poignee 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="FounItem"></param>
        /// <returns></returns>
        private int CanvasHandleSelect(int x, int y, ref VidPath FoundItem)
        {
            int retour = -1;

            double mindist = 0;
            VidPath NewFoundItem = null;

            foreach (VidPath v in this.m_paths)
            {
                double distance = -1;

                int resu = v.TstIsHover(x, y, ref distance);
                if (resu != -1)
                {
                    if (distance < mindist || NewFoundItem == null)
                    {
                        mindist = distance;
                        NewFoundItem = v;
                        retour = resu;
                    }
                }
            }


            if (NewFoundItem != m_CurrentItem)
            {
                if (m_CurrentItem != null)
                    m_CurrentItem.Unflash();
                if (NewFoundItem != null)
                    NewFoundItem.Flash();
                m_CurrentItem = NewFoundItem;
            }

            FoundItem = m_CurrentItem;
            return retour;

        }
    }



    class UndoItem
    {
        public UndoItem(Int64 ID, string action)
        {this.ID = ID;
         this.action = action;
         this.todelete = false;
        }

        public bool todelete;
        public Int64 ID;
        public string action;
    }

    // generateur d'ID
    internal class IDGenerator
    { 
        static Int64 valeur =1;

        public static Int64 GetID()
        { return valeur++;
        }
    }

}

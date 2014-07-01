using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Documents;


// pour creer un nouveau tpe de figure :
/*il faut definir les fonctions de  mouvemetn de ligne
    de renvoi des poignees utiles et l'effet de deplacemetn d'une poignee
    le dessin des adorners. attention a bien tenir a jour des variables permettant de dessiner l'adorer en plein mouvement
 *  la fonction ishoverline
 * penser a creer les fonction startchange et undoexec pour le undo permettant d'annuler un mouvement
*/
// petit probleme : l'adorner attrape la souris donc on ne peut pas cliquer SUR un bouton

namespace MetraEdit
{
    abstract public class VidPath 
    {

        Int64 m_ID;
        int m_selected = -1;

        public void AddUndo(string lastring)
        {
            if (m_canvas != null)
                m_canvas.AddUndo(m_ID, lastring);
        }

        protected VidCanvas m_canvas=null;

        Brush m_defFillColor = Brushes.Transparent;
        Brush m_defLineColor = Brushes.Red;


        public abstract void AdornerRender(System.Windows.Media.DrawingContext drawingContext);

        // execution d'un undo generique
        public virtual bool UndoExec(string action)
        {
            if (action.Contains("create"))
                {
                m_canvas.DeleteItem(this);
                return true;
                }

          return false;
        }

        public Path getcanvas()
        {return m_lepath;
        }

        // constructeur
        protected VidPath(VidCanvas canvas1, Int64 ID)
        {
          m_ID = ID;
          Start = true;
          m_lepath = new Path();
          m_canvas = canvas1;
          m_defFillColor = canvas1.FillColor;
          m_defLineColor = canvas1.BorderColor;
          
          m_adorner=  new VidAdorner(this); // cree un adorner 
          m_adorner.IsHitTestVisible = false; // evirte que l'adorner masque la souris au dessin
        }

        public bool IsID(Int64 ID)
        {
            return (ID==m_ID);
         
        }
        public Path m_lepath;

        // la creation commencé
        public bool Start
        {
            get;
            set;
        }

        // fait flasher le dessin
        virtual public void Flash()
        { }

        virtual public void Unflash()
        { }
        
        
        AdornerLayer aLayer = null;
        Adorner m_adorner = null;


        protected void AdornerUpdate()
        {
            m_lepath.InvalidateVisual();
        }


        int AddAdorner()
        {
            aLayer = AdornerLayer.GetAdornerLayer(m_lepath);
            aLayer.Add(m_adorner);
            return 0;
        }

        int DelAdorner()
        {
            if (aLayer != null)
                aLayer.Remove(m_adorner); // vire l'adorner
            return 0;

        }

        bool m_Selected= false;
        int m_SelectedHandle=-1;

        public void SetSelected(int numpoign, bool onoff)
        {
            DelAdorner();

            m_Selected = onoff;

        if (onoff)
        {
            m_SelectedHandle = numpoign;
            AddAdorner();
        }
        else
        {
            m_SelectedHandle = -1;
            DelAdorner();
        }
        }


        protected abstract bool IsHoverLine(int hx, int hy, ref double dist);
        
        // renvoie si on est pres de la figure
        // soit une poignee
        // soit la ligne
        // -1 si rien

        public int TstIsHover(int x, int y, ref double distance)
        {
            double distpoignees=-1;
            double distligne=-1;


            // ici mettre une condition suivant le mode edit
            if (this.m_Selected)
            {
                int poignee = tstpoignees(x, y, ref distpoignees);
                if (poignee > 0) //&& (overline == false || distligne >= distpoignees))
                {
                    distance = distpoignees;
                    return poignee;
                }
            }

            bool overline = IsHoverLine(x, y, ref distligne);
            if (overline)
                {
                    distance = distligne;
                    return 0; // on est pres de la ligne
                }
                else
                {
                    distance = -1;
                    return -1;
                }
        }



        // par defaut ca sauvegarde la position 
        // exceptionnellement non (cas de la creation)
        public void StartChange()
        {StartChange(true);
        }


        public abstract void StartChange(bool SavUndo);
        
        // deplacement d'une poignee
        public abstract bool MovePoignee(int numpoign, int dx, int dy);


        public abstract bool MoveLine(int dx, int dy);
            
        // renvoie le numero de la prochaine poignee a creer
        // uniquement pour les figures a nombre de pointsd variables
        public virtual int GetNxtHandleCreate(int numhandle)
        {// cas le plus ourant d'un objet a 2 poignees.
            // tout le reste doit etre surcharge
            if (numhandle == 0)
                return 1;

            return 0;

        }
        

        // renvoie la prochaine poignee sensible au hover
        protected abstract int GetNxtHandleValid(int numhandle, ref double hx, ref double hy);
        
        // teste toutes les poignees par rapport au mopint hx, hy
        int tstpoignees(int hx, int hy, ref double dist)
        {int foundpoign =0;
         double minlength = 0;
         
         Point tstpt = new Point(hx,hy);
         int numpoign = 0;
         double x =0;
         double y =0;

         while ((numpoign = GetNxtHandleValid(numpoign, ref x, ref y)) != 0) 
             {Point pt = new Point(x,y); // coord dde la poignee
              Vector v = tstpt - pt;
              Double lenvect = Math.Abs(v.Length);
              if (lenvect <= (m_canvas.HandleSensDistance + m_canvas.HandleSize))  // seuil attrape des poignees
                 if (lenvect < minlength || foundpoign == 0) // premiere poignee ou poignee la plus proche 
                    {
                    minlength = lenvect;
                    foundpoign = numpoign;
                    }
              }

        dist = minlength;
        return foundpoign;
        } 

    }

    //---------------------------------------------
    // classe derivee rectangle
    //---------------------------------------------
    class VidRect : VidPath
    {
        

        // ici les elements mis a jour qd on dessine l'adorner

        Point m_ATopLeft = new Point();
        Point m_ABottomRight = new Point();
        
        public VidRect(VidCanvas canvas1, Int64 ID, Point pt)            
            :base(canvas1,ID)
        {            
         Point Pt1 = pt;
         Point Pt2 = pt;
        
         m_lepath.Fill = canvas1.FillColor;
         m_lepath.Stroke = canvas1.BorderColor;
         RectangleGeometry rg = new RectangleGeometry();
         double width = 0;
         double height = 0;
         double left = pt.X;
         double top = pt.Y;
         rg.Rect = new Rect(left, top, width, height);
         m_lepath.Data = rg;
         }


        public override bool UndoExec(string action)
        {

            if (action.Contains("move"))
            {
                string[] args = action.Split(' ', ';', ':');
                int topleftx = int.Parse(args[1]);
                int toplefty = int.Parse(args[2]);
                int bottomrightx = int.Parse(args[3]);
                int bottomrighty = int.Parse(args[4]);
                Point topleft = new Point(topleftx, toplefty);
                Point botomright = new Point(bottomrightx, bottomrighty);
                
                // mise a jour avleurs adorner
                m_ATopLeft.X = topleft.X;
                m_ATopLeft.Y = topleft.Y;
                m_ABottomRight.X = botomright.X;
                m_ABottomRight.Y = botomright.Y;
                
                Geometry geom = m_lepath.Data;
                RectangleGeometry rect = (RectangleGeometry)geom;
                rect.Rect = new Rect(topleft, botomright);
                return true;
                }

            // sinon on va executer le parent
            return base.UndoExec(action);
        }

        Rect m_startrect;

        
        // sauvegarde de l'objet au demarrage d'edition
        override public void StartChange(bool savundo)
        {
            m_startrect = ((RectangleGeometry)m_lepath.Data).Rect;
            if (savundo)
            {
                string action = "move " + m_startrect.TopLeft.ToString() + ":" + m_startrect.BottomRight.ToString();
                AddUndo(action);
            }
        
        }




        // fait flasher le dessin
        override public void Flash()
        { }

        override public void Unflash()
        { }


        //--------------------------------
        // renvoie l'enum des poignees
        //--------------------------------
        protected override int GetNxtHandleValid(int numhandle, ref double hx, ref double hy)
        {
            Rect rect = ((RectangleGeometry)m_lepath.Data).Rect;

            switch (++numhandle)
            {
                case 1: hx = rect.TopLeft.X; hy = rect.TopLeft.Y; return numhandle; 
                case 2: hx = rect.TopRight.X; hy = rect.TopRight.Y; return numhandle; 
                case 3: hx = rect.BottomRight.X; hy = rect.BottomRight.Y; return numhandle; 
                case 4: hx = rect.BottomLeft.X; hy = rect.BottomLeft.Y; return numhandle; 
                case 5: return 0;
            }
            
            return 0; 
        }

        //-------------------------------------------
        // dit si le point est sur les lignes
        // et renvoie la plus proche distance a une ligne
        //-------------------------------------------
        protected override bool IsHoverLine(int hx, int hy, ref double dist)
        {double dist1,dist2;

          Rect rect = ((RectangleGeometry)m_lepath.Data).Rect;

          if (hx < rect.TopLeft.X - m_canvas.LineSensDistance || hx > rect.TopRight.X + m_canvas.LineSensDistance)
              return false;

          dist1 = Math.Min(Math.Abs(hy - rect.TopLeft.Y),Math.Abs(hy - rect.BottomLeft.Y));

          if (hy < rect.TopLeft.Y - m_canvas.LineSensDistance || hy > rect.BottomLeft.Y + m_canvas.LineSensDistance)
              return false;

          dist2 = Math.Min(Math.Abs(hx - rect.TopLeft.X),Math.Abs(hx - rect.TopRight.X));

          dist = Math.Min(dist1, dist2);

          if (dist < m_canvas.LineSensDistance)
              return true;

            return false;
        }



        //------------------------------------
        // deplace la poignee de dx et dy
        //------------------------------------
        public override bool MovePoignee(int numpoign, int dx, int dy)
            {
            bool retour = false;

        

            Point topleft = m_startrect.TopLeft;
            Point topright = m_startrect.TopRight;
            Point botomleft = m_startrect.BottomLeft;
            Point botomright = m_startrect.BottomRight;
            Point Delta = new Point(dx, dy);
        switch (numpoign)
            {
             
            case 1:  // topleft
               topleft -= (Vector)Delta;
               topright.Y -= Delta.Y;
               botomleft.X -= Delta.X;
               retour = true;
               break;

            case 2: //topright
                topright -= (Vector)Delta;
                topleft.Y -= Delta.Y;
                botomright.X -= Delta.X;
                retour = true;
                break;

            case 3: //bottomright
                botomright -= (Vector)Delta;
                botomleft.Y -= Delta.Y;
                topright.X -= Delta.X;
                retour = true;
                break;

            case 4: //bottomleft
                botomleft -= (Vector)Delta;
                botomright.Y -= Delta.Y;
                topleft.X -= Delta.X;
                retour = true;
                break;
            }


        Geometry geom = m_lepath.Data;
        RectangleGeometry rect = (RectangleGeometry)geom;
            // il n'y a que dans ce rectangle que les 4 points sont a jour. 
        rect.Rect = new Rect(topleft,botomright);
        m_ATopLeft.X = topleft.X;
        m_ATopLeft.Y = topleft.Y;
        m_ABottomRight.X = botomright.X;
        m_ABottomRight.Y = botomright.Y;

        AdornerUpdate();
        return retour;
        }


        // dessin de l'adorener
        /// <summary>
        /// 
        /// </summary>
        /// <param name="drawingContext"></param>
        public override void AdornerRender(System.Windows.Media.DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(m_ATopLeft - new Vector(-2, -2), m_ATopLeft - new Vector(+2, +2)));
            Point TopRight = new Point(m_ABottomRight.X, m_ATopLeft.Y);
            drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(TopRight - new Vector(-2, -2), TopRight - new Vector(+2, +2)));
            
            drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(m_ABottomRight - new Vector(-2, -2), m_ABottomRight - new Vector(+2, +2)));
            Point BottomLeft = new Point(m_ATopLeft.X, m_ABottomRight.Y);
            drawingContext.DrawRectangle(Brushes.Red, null, new System.Windows.Rect(BottomLeft - new Vector(-2, -2), BottomLeft - new Vector(+2, +2)));
            
        }



        //------------------------------------
        // deplace la poignee de dx et dy
        //------------------------------------
        public override bool MoveLine(int dx, int dy)
        {
            bool retour = false;

            Point topleft = m_startrect.TopLeft;
            Point botomright = m_startrect.BottomRight;
            Point Delta = new Point(dx, dy);
            topleft -= (Vector)Delta;
            botomright -= (Vector)Delta;
            Geometry geom = m_lepath.Data;
            RectangleGeometry rect = (RectangleGeometry)geom;
            rect.Rect = new Rect(topleft, botomright);

            m_ATopLeft.X = topleft.X;
            m_ATopLeft.Y = topleft.Y;
            m_ABottomRight.X = botomright.X;
            m_ABottomRight.Y = botomright.Y;

            AdornerUpdate();

            return true;
        }
    
    
    }


    class VidAdorner : Adorner
    {
        VidPath m_lepath=null;

        
        public VidAdorner(VidPath lepath)
            :base(lepath.getcanvas())
        { m_lepath = lepath;
        }


        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            m_lepath.AdornerRender(drawingContext);
        }

    }
}

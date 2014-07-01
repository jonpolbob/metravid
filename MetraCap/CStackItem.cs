using System;
using System.Collections.Generic;

using System.Text;

namespace LiveImage
{
    public class CStackItem
    {

        int sizex=640; 
        int sizey=480;
        int sizebuffer = 0;
        byte[] LeBuffer=new byte[640*480*4];
        
        public CStackItem()
        { 
        }
        
        byte fond = 0;

        public void GetBuffer(ref int sizex, ref int sizey, out byte[] LeBuffer)
        {sizex = this.sizex;
         sizey = this.sizey;
         LeBuffer = this.LeBuffer;
         
        }

        public void SavBuffer(int sizex, int sizey, byte[] LeBuffer)
        {
         /*   for (int i = 0; i < sizex * sizey * 4; i++)
                LeBuffer[i] = fond;
            if (fond < 130)
                fond += 10;
            else
                fond = 0;
         */
         this.sizex = sizex;
         this.sizey = sizey;
         this.LeBuffer = LeBuffer;
         
        }

    }
}

using System;
using System.Collections.Generic;

using System.Text;

namespace DShowCap
{
    class DeviceCreateException : ApplicationException
    {
        private string legend;
        private int numexception;


        /// <summary>
        /// recupere la legende
        /// </summary>
        public string Legende
        {
            get { return legend; }
        }

        /// <summary>
        /// constructeur
        /// </summary>
        /// <param name="numexception"></param>
        /// <param name="legend"></param>
        public DeviceCreateException(int numexception, string legend)
        {
            this.legend = legend;
            this.numexception = numexception;
        }
    }
}

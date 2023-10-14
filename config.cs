using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanHH
{
    internal class Config
    {
        /// <summary>
        /// Сайт для сканирования
        /// </summary>
        public string Target { set; get; } = "";

        /// <summary>
        /// Выходной файл
        /// </summary>
        public string OutFile { set; get; } = "out.csv";

        /// <summary>
        /// Сетевая задержка
        /// </summary>
        public int Pause    { set; get; } = 200;

        public int MaxList { set; get; } = 40;

        public Config() 
        { 
        
        }

    }
}

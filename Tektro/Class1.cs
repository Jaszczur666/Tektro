using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Text;

namespace Tektro
{
    public class punkt
    {
        public double x;
        public double y;
        public punkt()
        {
            x = 0;
            y = 0;
        }
    }
    public class curve
    {
        public List<punkt> decay;
        public double exc;
        public curve()
        {
            decay = new List<punkt>();
            exc = 0;
        }
    }
    public class Scope
    {
        private TekVISANet.VISA TVA;
        private bool initialized;
        public Scope()
        {
            TVA = new TekVISANet.VISA();
            initialized = false;
        }
        public void Initialize()
        {
            System.Collections.ArrayList instrlist;
            string response;
            bool status;
            TVA.FindResources("?*", out instrlist);
            Console.WriteLine("Visa Resources");
            for (int j = 0; j < instrlist.Count; j++)
            {
                Console.WriteLine(j.ToString() + " : " + instrlist[j]);
            }
            Console.WriteLine("\n");
            // Connect to a known instrument and print its IDN
            TVA.Open("USB::0x0699::0x0453::C021155::INSTR");
            TVA.Write("*IDN?");
            status = TVA.Read(out response);
            if (status)
            {
                Console.WriteLine(response);
                initialized = true;
            }
        }
        public bool Query(string query, out string response)
        {
            bool state = false;
            response = "Invalid response";//Will be changed by next line if it's succesful. If not - this line is true;
            if (initialized) state = TVA.Query(query, out response);
            return state;
        }
        public bool Write(string data)
        {
            bool state = false;
            if (initialized) state = TVA.Write(data);
            return state;
        }
        public void resetAcquisition() {
            if (initialized) {
                //TVA.Write("ACQ:STATE OFF");
                TVA.Write("ACQuire:NUMAVg 8");
                Thread.Sleep(10);
                TVA.Write("ACQuire:NUMAVg 64");

            };
        }
        public void dumpAscii(out string response)
        {
            double xincr, ymult, yzero, yoff, xpos, xdel;
            response = "";
            int[] rawwave;
            double[] wave;
            TVA.Write("DATA:SOU CH1");
            TVA.Write("DATA:WIDTH 2");
            TVA.Write("DATA:ENC ASC");
            TVA.Query("WFMPRE:YMULT?", out response);
            Console.WriteLine(response);
            ymult = float.Parse(response, CultureInfo.InvariantCulture);
            TVA.Query("WFMPRE:YZERO?", out response);
            Console.WriteLine(response, CultureInfo.InvariantCulture);
            yzero = float.Parse(response, CultureInfo.InvariantCulture);
            TVA.Query("WFMPRE:YOFF?", out response);
            Console.WriteLine(response);
            yoff = float.Parse(response, CultureInfo.InvariantCulture);
            TVA.Query("WFMPRE:XINCR?", out response);
            Console.WriteLine(response);
            xincr = float.Parse(response, CultureInfo.InvariantCulture);
            TVA.Query("CURVE?", out response);
            List<int> TagIds = response.Split(',').Select(int.Parse).ToList();
            rawwave = new int[TagIds.Count];
            for (int i = 0; i < TagIds.Count; i++) rawwave[i] = TagIds[i];
            //Console.WriteLine(response);
            //Console.WriteLine("yz " + yzero + " yoff " + yoff + " ymult " + ymult + " xinc " + xincr);
            //Console.WriteLine("Number of Points " + rawwave.Count());
            //Console.WriteLine(rawwave[0].ToString());
            //Console.WriteLine(rawwave[1].ToString());
            wave = new double[rawwave.Count()];
            for (int j = 0; j < rawwave.Count(); j++)
            {
                wave[j] = (rawwave[j] - yoff) * ymult + yzero;

            }
            TVA.Query("HOR:POS?", out response);
            Console.WriteLine(response);
            xpos = double.Parse(response, CultureInfo.InvariantCulture);
            xdel = xpos * xincr * rawwave.Count() / 100;
            response = "";
            for (int j = 0; j < wave.Count(); j++)
            {
                double timepoint = j * xincr - xdel;
                response += timepoint.ToString() + " " + wave[j].ToString() + "\r\n";
            }
            //Console.WriteLine("ASCII Dumped ");

        }
        public void dumpList(out curve decaycurve) 
        {
            string res;
            decaycurve = new curve();
            dumpAscii(out res);
            //Console.WriteLine("Huge load of data hopefully follows");
            //Console.WriteLine("___________________________________");
            //Console.WriteLine(res);
            //Console.WriteLine("___________________________________");
            //res = System.IO.File.ReadAllText("log.txt");
            var result = System.Text.RegularExpressions.Regex.Split(res, "\r\n|\r|\n");
            
            foreach (string line in result)
            {
                
                //Console.WriteLine(line);
                if (line != "")
                {
                    punkt point = new punkt();
                    var sub = System.Text.RegularExpressions.Regex.Split(line, " ");
                    point.x = double.Parse(sub[0]);
                    point.y = double.Parse(sub[1]);
                    //Console.WriteLine(sub[0] + ":" + sub[1]);
                    //Console.WriteLine(point.x + " " + point.y);
                    decaycurve.decay.Add(point);
                }
                
            }
        }
        public void Close()
        {
            TVA.Clear();
            TVA.Close();

        }
    }
};

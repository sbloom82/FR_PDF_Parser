using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FR_PRF_Parser
{
    public class DTC
    {
        public string GlobalError;
        public string ErrorLocation;
        public string Description;
        public string Sym;
        public string DFCCode;
        public string SAECode;
        public string VAGCode;
        public string ErrorType;
        public string ErrorClass;
        public string Carb;//??

        [XmlIgnore]
        public int DFCIndex;

        public static DTC GetStartDTC(string[] values)
        {
            if (values.Length > 5)
            {
                for (int i = values.Length - 1; i > 5; --i)
                {
                    string pcode1 = values[i - 4];
                    string pcode2 = values[i - 3];
                    string dfc = values[i - 5];

                    if (IsPCode(pcode1) && IsPCode(pcode2) &&
                        (dfc == "tbd" || (dfc.Length == 4 && short.TryParse(dfc, out short dfcValue))))
                    {
                        DTC dtc = new DTC()
                        {
                            DFCCode = dfc,
                            SAECode = pcode1,
                            VAGCode = pcode2,
               
                            ErrorClass = values[i - 1],
                            Carb = values[i],

                            DFCIndex = i - 5
                        };

                        dtc.ErrorType = values[i - 2];

                        return dtc;
                    }
                }
            }
            return null;
        }

        public static bool IsErrorName(string value)
        {
            foreach (char c in value)
            {
                if (!char.IsUpper(c) && !char.IsNumber(c) && c != '_' && c != '/' && c != '(' && c != ')' && c != 'i')
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsPCode(string value)
        {
            return value.Length == 5 && (value.StartsWith("P") || value.StartsWith("U"));
        }

    }
}

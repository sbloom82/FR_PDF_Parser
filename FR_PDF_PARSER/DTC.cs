using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public int DFCIndex;

        public static DTC GetStartDTC(string[] values)
        {
            if (values.Length > 5)
            {
                string pcode1 = values[values.Length - 5];
                string pcode2 = values[values.Length - 4];
                string dfc = values[values.Length - 6];

                if (IsPCode(pcode1) && IsPCode(pcode2) && dfc.Length == 4 && short.TryParse(dfc, out short dfcValue))
                {
                    return new DTC()
                    {
                        DFCCode = dfc,
                        SAECode = pcode1,
                        VAGCode = pcode2,
                        ErrorType = values[values.Length - 3],
                        ErrorClass = values[values.Length - 2],
                        Carb = values[values.Length - 1],

                        DFCIndex = values.Length - 6
                    };
                }
            }
            return null;
        }

        public static bool IsErrorName(string value)
        {
            foreach (char c in value)
            {
                if (!char.IsUpper(c) && c != '_')
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

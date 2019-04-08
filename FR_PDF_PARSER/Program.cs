using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FR_PRF_Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            //PassOne(args);

            PassTwo(args);
        }


        public static void PassOne(string[] args)
        {
            PdfReader reader = new PdfReader(@"C:\Users\steve\Source\Repos\fr-pdf-parser\FR_PDF_PARSER\Siemens 3.0T Audi S4, S5, A6, Q7.pdf");

            using (StreamWriter sw = new StreamWriter(@"C:\temp\UDSVars.txt"))
            {
                for (int page = 1366; page <= 1402; page++)
                {

                    string pageText = PdfTextExtractor.GetTextFromPage(reader, page + 1);
                    sw.Write(pageText);


                }
            }
            reader.Close();

            using (StreamWriter sw = new StreamWriter(@"C:\temp\UDSVars.cs"))
            {
                string[] lines = File.ReadAllLines(@"C:\temp\UDSVars.txt");
                foreach (string l in lines)
                {
                    string line = l.TrimStart();

                    if (line.StartsWith("0x"))
                    {
                        if (line.Length > 5 &&
                            line[2] != ' ' &&
                            line[3] != ' ' &&
                            line[4] != ' ' &&
                            line[5] != ' ')
                        {
                            string address = line.Substring(0, 6);
                            string name = "";

                            sw.WriteLine("new Variable() {Address = " + address + ", Name = \"" + name + "\"},");
                        }
                    }
                }
            }
        }

        public static void PassTwo(string[] args)
        {
            PdfReader reader = new PdfReader(@"C:\Users\steve\Source\Repos\fr-pdf-parser\FR_PDF_PARSER\Siemens 3.0T Audi S4, S5, A6, Q7.pdf");

            bool dataDefStarted = false;

            for (int page = 1; page <= reader.NumberOfPages; ++page)
            {
                string pageText = PdfTextExtractor.GetTextFromPage(reader, page);
                string[] lines = pageText.Split(new char[] { (char)0x0A }, StringSplitOptions.None);

                for (int i = 0; i < lines.Length; ++i)
                {
                    string line = lines[i].Trim();
                    if (dataDefStarted)
                    {
                        if (line.StartsWith("Input Data:", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //Console.WriteLine("End of Data Definition Section");
                            dataDefStarted = false;
                        }
                        else
                        {
                            if (DetectVariableName(line))
                            {
                                ParseVariable(lines, ref i);
                            }
                        }
                    }
                    else
                    {
                        if (line.StartsWith("Data Definition:", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // Console.WriteLine("Begin of Data Definition Section");
                            dataDefStarted = true;
                        }
                    }
                }
            }

            reader.Close();

            List<Variable> parsed = Variables.variables.Where(v => v.Parsed).OrderBy(v => v.Address).ToList();
            XmlSerializer ser = new XmlSerializer(typeof(List<Variable>));
            using (FileStream fs = new FileStream(@"c:\temp\parsedVariables.xml", FileMode.Create))
            {
                ser.Serialize(fs, parsed);
            }

            List<Variable> unparsed = Variables.variables.Where(v => !v.Parsed).OrderBy(v => v.Address).ToList();
            using (FileStream fs = new FileStream(@"c:\temp\unparsedVariables.xml", FileMode.Create))
            {
                ser.Serialize(fs, unparsed);
            }

            Console.WriteLine("Complete.  Press any Key to continue");
            Console.ReadKey();
        }


        private static int minVarNameLength = 3;
        private static bool DetectVariableName(string line)
        {
            if (line.Length >= minVarNameLength)
            {
                for (int i = 0; i < minVarNameLength; ++i)
                {
                    if (line[i] != '_' && !char.IsUpper(line[i]) && !char.IsDigit(line[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        static char[] splitCharArray = new char[] { ' ' };
        private static bool ParseVariable(string[] lines, ref int currentLineIndex)
        {
            int currentLineAdvance = 0;
            string currentLine = lines[currentLineIndex];

            //get full name of current variable
            string variableName = string.Empty;

            string[] parts = currentLine.Split(splitCharArray, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 6)
            {
                if (parts[1].StartsWith("["))
                {
                    variableName = parts[0] + "[";
                }
                else
                {
                    string nextLine = "";
                    try { nextLine = lines[currentLineIndex + 1]; } catch { }
                    if (nextLine.StartsWith("["))
                    {
                        variableName = parts[0] + "[";
                        //need to advance line counter one more position to get description
                        currentLineAdvance++;
                    }
                    else if (parts[0].EndsWith("(X)")) //faaacck you!
                    {
                        variableName = parts[0].Replace("(X)", "[");
                    }
                    else
                    {
                        variableName = parts[0];
                    }
                }
            }

            if (string.IsNullOrEmpty(variableName))
            {
                return false;
            }

            IEnumerable<Variable> matchingVars = Variables.variables.Where(v =>
                (variableName.EndsWith("[") ? v.Name.StartsWith(variableName) : v.Name.Equals(variableName)) &&
                !v.Parsed);

            if (matchingVars.Any())
            {
                Console.WriteLine($"Matching Varibles found for line: {currentLine} => {string.Join(", ", matchingVars.Select(v => v.Name))}");

                int lineIndex = parts[1].StartsWith("[") ? 2 : 1;
                string mode = parts[lineIndex++];
                string hexLimits = "";
                string physLimits = "";
                string resolution = "";
                string unit = "";
                string description = "";

                try
                {
                    hexLimits = parts[lineIndex++] + parts[lineIndex++];
                    if (!hexLimits.EndsWith("H"))
                    {
                        lineIndex -= 2;
                        string[] nextLineSplit = lines[currentLineIndex + 1].Split(splitCharArray, StringSplitOptions.RemoveEmptyEntries);
                        hexLimits = parts[lineIndex++];
                        if (currentLineAdvance > 0)
                        {
                            //name was multilined
                            hexLimits += nextLineSplit[1];
                        }
                        else
                        {
                            currentLineAdvance++;
                            hexLimits += nextLineSplit[0];
                        }
                    }


                    physLimits = parts[lineIndex++] + parts[lineIndex++];
                    if (physLimits.EndsWith("...") || physLimits.IndexOf("...") == -1)
                    {
                        lineIndex -= 2;
                        string[] nextLineSplit = lines[currentLineIndex + 1].Split(splitCharArray, StringSplitOptions.RemoveEmptyEntries);
                        physLimits = parts[lineIndex++];
                        if (currentLineAdvance == 0)
                        {
                            currentLineAdvance++;
                            physLimits += nextLineSplit[0];
                        }
                        else
                        {
                            physLimits += nextLineSplit[nextLineSplit.Length == 1 ? 0 : (nextLineSplit.Length < 3 ? 1 : 2)];
                        }
                    }

                    resolution = parts[lineIndex++];
                    if (GetResolution(resolution) == 0)
                    {
                        lineIndex -= 3;
                        string[] nextLineSplit = lines[currentLineIndex + 1].Split(splitCharArray, StringSplitOptions.RemoveEmptyEntries);
                        physLimits = parts[lineIndex++];
                        if (currentLineAdvance == 0)
                        {
                            currentLineAdvance++;
                            physLimits += nextLineSplit[0];
                        }
                        else
                        {
                            physLimits += nextLineSplit[nextLineSplit.Length == 1 ? 0 : (nextLineSplit.Length < 3 ? 1 : 2)];
                        }
                        resolution = parts[lineIndex++];
                    }

                    unit = parts[lineIndex];
                    description = lines[currentLineIndex + currentLineAdvance + 1];

                    foreach (Variable v in matchingVars)
                    {
                        v.Size = GetSize(hexLimits);
                        v.Signed = !hexLimits.StartsWith("0") || physLimits.StartsWith("-");
                        v.MinValue = GetMin(physLimits);
                        v.MaxValue = GetMax(physLimits);
                        v.Resolution = GetResolution(resolution);
                        v.Unit = unit;
                        v.Description = description;
                        v.Parsed = true;
                        Console.WriteLine($"**********Successfully parsed {v.Name}*************");
                    }

                    currentLineIndex += currentLineAdvance;
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing data... manually fix up values for {variableName} - {currentLine}");
                }
            }

            return false;
        }

        private static short GetSize(string hexLimits)
        {
            string[] parts = hexLimits.Split(new string[] { "..." }, StringSplitOptions.None);
            if (parts[1].Length <= 3)
            {
                return 1;
            }
            else if (parts[1].Length > 5)
            {
                return 4;
            }
            return 2;
        }

        private static decimal GetMin(string physicalLimits)
        {
            string[] parts = physicalLimits.Split(new string[] { "..." }, StringSplitOptions.None);
            return decimal.Parse(parts[0]);
        }

        private static decimal GetMax(string physicalLimits)
        {
            string[] parts = physicalLimits.Split(new string[] { "..." }, StringSplitOptions.None);
            return decimal.Parse(parts[1]);
        }

        private static decimal GetResolution(string resolution)
        {
            if (!decimal.TryParse(resolution, out decimal value))
            {
                try { value = decimal.Parse(resolution, System.Globalization.NumberStyles.Float); } catch { }
            }
            return value;

        }

    }
}

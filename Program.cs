using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveCost2EA
{
    public class Program
    {
        private static string[] helpStringArr =
        {
            "MoveCost2EA. Usage:",
            "./movecost2ea <inputFilename> [outputFilename]",
            "",
            "Available options:",
            "--help",
            "Displays this message.",
            "See online instructions for more information."
        };
        private static readonly string helpString = System.Linq.Enumerable.Aggregate(helpStringArr,
            (string a, string b) => { return a + '\n' + b; });

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Out.WriteLine("ERROR: Too few arguments. Use '--help' for more information.");
                return 1;
            }
            if (args.Length > 2)
            {
                Console.Out.WriteLine("ERROR: Too many arguments. Use '--help' for more information.");
                return 1;
            }

            if (args[0] == "-h" || args[0] == "--help")
            {
                Console.Out.WriteLine(helpString);
                return 0;
            }
            //Console.Out.WriteLine("test");
            string ifile = args[0];
            string ofile;
            if (args.Length == 2) { ofile = args[1]; } else { 
                ofile = ifile;
                //strip down ifile into ofile directory, then append ofile filename
                if (ofile.Contains("/")) { ofile = ofile.Substring(0, ofile.LastIndexOf('/') + 1); }
                else if (ofile.Contains("\\")) { ofile = ofile.Substring(0, ofile.LastIndexOf('\\') + 1); }
                else { ofile = ""; }
            }

            ofile = String.Concat(ofile, "MoveCostsInstaller.event");

            //get all lines from all included files in order in an array list
            ArrayList ilines = getLines(ifile);

            //now we can parse a line at a time
            //set up some objects first
            DefinitionDict defs = new DefinitionDict(); //hashtable of definitions and their values
            List<MoveCostTable> tables = new List<MoveCostTable>(); //list of MoveCostTable objects
            MoveCostTable table = new MoveCostTable(); //current move cost table
            bool inTable = false; //whether or not we're currently parsing in a table
            char[] whitespace = {' ', '\t', '\n', '\r'};


            //before we do this go through every line once and delete comments from the end where applicable
            
            for (int i = 0; i < ilines.Count; i++)
            {
                //ilines[i] = (string)ilines[i] + "\n";
                string line = (string)ilines[i];
                if (line.Length < 3) continue;
                for (int j = 0; j < line.Length-2; j++)
                {
                    //Console.Out.WriteLine("Testing from " + j + " to " + (j+1) + ": " + line.Substring(j,2));
                    if (line.Substring(j,2).Equals("//"))
                    {
                        ilines[i] = line.Substring(0,j);
                        break;
                    }
                }
            }

            
            


            foreach (string line in ilines) {

                //is the current line empty?
                if (line.Trim().Length == 0) continue;

                //otherwise, prepare to parse the line
                string curLine = line.TrimStart(); //needs to be not the foreach variable so we can edit it

                if (!inTable)
                {
                    if (curLine.Substring(0, curLine.IndexOfAny(whitespace)).Equals("#include")) continue;

                        //is it a definition?
                        if (curLine.Substring(0, curLine.IndexOfAny(whitespace)).Equals("#define"))
                    {
                        curLine = curLine.TrimStart();
                        int next = curLine.IndexOfAny(whitespace);
                        if (next == -1) next = 1;   //TODO: throw an error
                        curLine = curLine.Substring(next).TrimStart();
                        string defName = curLine.Substring(0,curLine.IndexOfAny(whitespace));
                        next = curLine.IndexOfAny(whitespace);
                        curLine = curLine.Substring(next).TrimStart();
                        string defVal = curLine.TrimEnd();

                        defs.addDef(defName, defVal);
                    }
                    else //it's the start of a new group
                    {
                        curLine = curLine.TrimStart();
                        string groupName = curLine.Substring(0, curLine.IndexOfAny(whitespace)).Trim() ;

                        if (tableExists(groupName,tables))
                        {
                            Console.Out.WriteLine("ERROR: Redefining movegroup " + groupName + ".");
                            return -1;
                        }

                        table = new MoveCostTable();
                        table.setName(groupName);

                        int next = curLine.IndexOfAny(whitespace);
                        if (next == -1) next = 1;
                        curLine = curLine.Substring(next).TrimStart();
                        if (curLine.Length > 7 && curLine.Substring(0, 7).Equals("imports"))
                        {
                            curLine = curLine.Substring(curLine.IndexOfAny(whitespace)).TrimStart();
                            string importName = curLine.Substring(0, curLine.IndexOfAny(whitespace));
                            if (!tableExists(importName,tables))
                            {
                                Console.Out.WriteLine("ERROR: Attempting to import undefined movegroup " + importName + " to movegroup " + groupName + ".");
                                return -1;
                            }
                            table.copyCosts(getTableByName(importName, tables));
                            next = curLine.IndexOfAny(whitespace);
                            if (next == -1) next = 7;
                            curLine = curLine.Substring(curLine.IndexOfAny(whitespace)).TrimStart();
                        }  
                      
                        if (!curLine.Trim().Equals("{"))
                        {
                            Console.Out.WriteLine("ERROR: missing '{' after defining movegroup " + groupName + ".");
                            return -1;
                        }
                        inTable = true;
                        continue;
                    }

                } else
                {
                    //in table, so the value of table is not null
                    //things we can have here:
                    // A = B    //set value in table at index A to value B 
                    // A + B    //set value in table at index A to current value + B
                    // A - B    //set value in table at index A to current value - B
                    // A * B    //set value in table at index A to current value * B
                    // A / B    //set value in table at index A to current value / B

                    // [A] = B  //set value in table at index A to value B
                    // [A,B] = C //set value at each index in array [A,B] to value C; works for an infinite amount of arguments in brackets
                    // [A-B] = C //set value at each index in array [A-B] to value C

                    // A = GroupName[B] //set value in table at index A to value in table GroupName at index B
                    // [A-B] = GroupName[A-B] //set value at each index in array [A-B] to value in table GroupName at each index in array [A-B]; both arrays need to be of the same length for this one to work
                    // [A,B] = GroupName[A,B] //set value at each index in array [A,B] to value in table GroupName at each index in array [A,B]; this also works with the same length restrictions

                    // }    //end of the group, add to tables list and flip inTable to false

                    //in all of these states, the first argument is either an index or an array of indices
                    //if the first argument of the line does not match the regex [0-9]+ or the regex \[([0-9]+,?)+\], look for a definition with the value of the argument; if it doesn't exist, error
                    //the second argument is always an operator. +-*/ are special cases that do specific things always as operators (and work with either arrays or values as the first argument)
                    //otherwise it's a simple value assignment, but the value in question depends on the rest of the statement
                    //in the event of an array, we want to parse the array and make a list of all elements within it. When we perform the operation, we perform it for each element in the list.
                    //otherwise, we perform the operation only once. For cohesion, this can be implemented as a list with only 1 entry performed on every element of the list.
                    //the rest of the line is the value operand. This is either a number or a reference to another table. 
                    //if the value operand is an array reference to another table, we have to verify it's the same length as the index operand array. In the case of a non-array index operand, 
                    //  We can implement this as lists with a single element and use the same code to avoid doubling up on code unnecessarily.
                    //then, we either apply 1 value to each of the values of a list, or map sequential values from 1 list to another and apply each.
                    //this is just list[i] = j or list[i] = list2[i], where list and list2 are our parsed lists of indices from the index and value array operands
                    //the second will go out of bounds on one of the arrays if the lists aren't the same size, which is why we earlier check for this and error if not met.

                    if (line.Trim().Equals("}"))
                    {
                        tables.Add(table);
                        inTable = false;
                        continue;
                    }

                    ArrayList inputArray = new ArrayList();
                    ArrayList valueArray = new ArrayList();


                    //Parse operands from line string first
                    int next = curLine.IndexOfAny(whitespace);
                    if (next == -1) next = 1;
                    string indexOperand = curLine.Substring(0, next);
                    curLine = curLine.Substring(next).TrimStart();
                    next = curLine.IndexOfAny(whitespace);
                    if (next == -1) next = 1;
                    string op = curLine.Substring(0, next);
                    curLine = curLine.Substring(next).TrimStart();
                    next = curLine.IndexOfAny(whitespace);
                    if (next == -1) next = curLine.Length;
                    string valueOperand = curLine.Substring(0, next);

                   

                    //verify validity of this; check indexOperand format, check operand is valid, check value is either an integer or the referenced group exists, if 2 arrays check for same length

                    //verify indexOperand format, then parse values from it
                    
                    //add to input array list initialized previously
                    inputArray.AddRange(parseArgument(indexOperand, defs));
                    

                    //verify valueOperand format, then parse values from it 
                    
                    //the start of this is either the name of a movegroup, something to be parsed as a definition, or an integer
                    //let's assume that if the line contains brackets that it's a reference to another movegroup

                    if (valueOperand.IndexOf('[') != -1)
                    {
                        //This operand begins with the name of a movegroup, then has an array to be parsed
                        int n = valueOperand.IndexOf('[');
                        if (n == -2) n = 0;
                        string groupName = valueOperand.Substring(0, n);
                        if (!tableExists(groupName,tables))
                        {
                            Console.Out.WriteLine("ERROR: Nonexistent movegroup " + groupName + ".");
                            return -1;
                        }
                        valueOperand = valueOperand.Substring(valueOperand.IndexOf('['));

                        ArrayList intermediateArray = new ArrayList();
                        intermediateArray.AddRange(parseArgument(valueOperand, defs));

                        MoveCostTable refTable = getTableByName(groupName,tables);

                        foreach (int value in intermediateArray)
                        {
                            valueArray.Add(refTable.getCost(value));
                        }


                    }
                    else
                    {
                        //This operand is a definition or an integer
                        valueArray = parseArgument(valueOperand,defs);
                    }

                    //if only 1 value, repeat to # of inputs in size
                    if (valueArray.Count == 1 && inputArray.Count != 1) 
                    {
                        int tmp = (int)valueArray[0];
                        for (int i = 0; i < inputArray.Count - 1; i++)
                        {
                            valueArray.Add(tmp);
                        }
                    }

                    //verify that both arrays are the same length
                    if (inputArray.Count != valueArray.Count)
                    {
                        Console.Out.WriteLine("ERROR: Incongruent input and value array sizes. " + inputArray.Count + " input(s) and " + valueArray.Count + " output(s).");
                        return -1;
                    }



                    //verify operand is valid, then perform an operation based on what it is
                    switch (op)
                    {
                        case "+":
                            for (int i = 0; i < inputArray.Count; i++) {
                                //add value in table at index from inputArray to value at index in valueArray, and store to table at index
                                int a = table.getCost((int)inputArray[i]);
                                int b = (int)valueArray[i];
                                table.addCost((int)inputArray[i],a+b);
                            }
                            break;
                        case "-":
                            for (int i = 0; i < inputArray.Count; i++)
                            {
                                //sub value in table at index from inputArray to value at index in valueArray, and store to table at index
                                int a = table.getCost((int)inputArray[i]);
                                int b = (int)valueArray[i];
                                table.addCost((int)inputArray[i], a - b);
                            }
                            break;
                        case "*":
                            for (int i = 0; i < inputArray.Count; i++)
                            {
                                //mul value in table at index from inputArray to value at index in valueArray, and store to table at index
                                int a = table.getCost((int)inputArray[i]);
                                int b = (int)valueArray[i];
                                table.addCost((int)inputArray[i], a * b);
                            }
                            break;
                        case "/":
                            for (int i = 0; i < inputArray.Count; i++)
                            {
                                //div value in table at index from inputArray to value at index in valueArray, and store to table at index
                                int a = table.getCost((int)inputArray[i]);
                                int b = (int)valueArray[i];
                                table.addCost((int)inputArray[i], a / b);
                            }
                            break;
                        case "=":
                            for (int i = 0; i < inputArray.Count; i++)
                            {
                                //set value in table at index from inputArray to value at index in valueArray
                                table.addCost((int)inputArray[i], (int)valueArray[i]);
                            }
                            break;
                        default:
                            Console.Out.WriteLine("ERROR: Invalid operator " + indexOperand + ".");
                            return -1;
                    }

                    //we have now finished processing this line, so continue to the next one
                    continue;

                }




            }

            // we have now finished assembling our table of tables, now we write the EA installer with the data as ofile 

            ArrayList installerText = new ArrayList();
            string installerHeader = "//Generated by MoveCost2EA\n";
            installerText.Add(installerHeader);

            foreach (MoveCostTable curTable in tables)
            {
                string contentLine = "BYTE ";
                installerText.Add(curTable.getName() + ":");
                for (int i = 0; i < 256; i++)
                {
                    contentLine += curTable.getCost(i) + " ";
                }
                installerText.Add(contentLine);
                installerText.Add("\n");
            }

            if (File.Exists(ofile)) File.Delete(ofile);
            File.OpenWrite(ofile).Close();
            File.WriteAllLines(ofile, (String[])installerText.ToArray(typeof(string)));

            Console.Out.WriteLine("Finished.");

            return 0;
        }

        public static ArrayList getLines(string file)
        {
            ArrayList lines = new ArrayList();
            foreach (string line in System.IO.File.ReadLines(file))
            {
                if ((line.Length > 8) && line.TrimStart().Substring(0,8).Equals("#include")) {
                    int lastIndex = file.LastIndexOf('/') + 1;
                    if (lastIndex == 0) lastIndex = file.LastIndexOf('\\') + 1;
                    if (lastIndex == 0) lastIndex = 1;
                    string appendedPath = line.Trim().Substring(8).Trim();
                    appendedPath = appendedPath.Substring(0, appendedPath.Length).Trim();
                    string newFile = file.Substring(0, lastIndex - 1) + appendedPath;
                    lines.AddRange(getLines(newFile));
                }
                else
                {
                    lines.Add(line);
                }
            }
            return lines;
        }

        public static bool tableExists(string name, List<MoveCostTable> tables)
        {
            foreach (MoveCostTable table in tables)
            {
                if (table.getName().Equals(name)) return true;
            }
            return false;
        }

        public static MoveCostTable getTableByName(string name, List<MoveCostTable> tables)
        {
            foreach (MoveCostTable table in tables)
            {
                if (table.getName().Equals(name)) return table;
            }
            return null;
        }

        public static ArrayList parseArgument(string input, DefinitionDict defs)
        {
            //input is a string that's the singular argument we're parsing
            //return an arraylist with the contents of the argument stored in it

            ArrayList retList = new ArrayList();

            if (input.IndexOf("[") == -1)
            {
                //not an array, so parse for definition value if needed and return result
                retList.Add(int.Parse((string)getDefinitionValue(input, defs)));
                return retList;
            }

            //this is an array
            input = input.Substring(1); //remove leading [

            while (input.Length > 0)
            {

                if (input.IndexOf('-') == -1 && input.IndexOf(',') == -1)
                {
                    if (input.IndexOf(']') == -1)
                    {
                        Console.Out.WriteLine("ERROR: missing ] at end of array.");
                    }
                    retList.Add(int.Parse((string)getDefinitionValue(input.Substring(0, input.IndexOf(']')), defs)));
                    input = "";
                    continue;
                }

                //is the next operator - or ,
                if (input.IndexOf('-') != -1)
                {
                    //it's -, so we want all values from the first of the next values to the second of the next values
                    int startValue = int.Parse((string)getDefinitionValue(input.Substring(0, input.IndexOf('-')),defs));
                    input = input.Substring(input.IndexOf('-')+1);
                    char c = ',';
                    if (input.IndexOf(',') == -1) c = ']';
                    int endValue = int.Parse((string)getDefinitionValue(input.Substring(0, input.IndexOf(c)), defs));
                    input = input.Substring(input.IndexOf(c) + 1);

                    while (startValue <= endValue)
                    {
                        retList.Add(startValue);
                        startValue++;
                    }

                }
                else
                {
                    //it's , so we want just the one value between here and the next , or ]
                    char c = ',';
                    if (input.IndexOf(',') == -1) c = ']';
                    retList.Add(int.Parse((string)getDefinitionValue(input.Substring(0, input.IndexOf(c)), defs)));
                    input = input.Substring(input.IndexOf(c) + 1);
                }


            }
            return retList;

        }

        public static Object getDefinitionValue(Object def, DefinitionDict defs)
        {
            if (!int.TryParse((string)def, out int i))
            {
                string newVal = (string)defs.getDef(def);
                if (newVal == null)
                {
                    Console.Out.WriteLine("ERROR: Undefined definition " + def + ".");
                    return null;
                }
                def = newVal;
                if (!int.TryParse((string)def, out int j)) def = getDefinitionValue(def, defs);
            }
            return def;
        } 

    }
}

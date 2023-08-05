using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoveCost2EA
{
    public class DefinitionDict
    {
        private Hashtable dict = new Hashtable();

        public Object getDef(Object id)
        {
            if (dict.ContainsKey(id)) return dict[id];
            return null;
        }

        public void addDef(Object id, Object value)
        {
            try
            {
                dict.Add(id, value);
            }
            catch
            {
                Console.WriteLine("WARNING: Redefining " + id.ToString() + ".");
                dict[id] = value;
            }
        }

    }
    public class MoveCostTable
    {
        private int[] costs = new int[256];
        private string name;

        public void setName(string s)
        {
            name = s;
        }
        public string getName()
        {
            return name;
        }
        public void addCost(int index, int cost)
        {
                costs[index] = cost;
        }
        public int getCost(int index)
        {
            return (int)costs[index];
        }
        public int[] getTable()
        {
            return costs;
        }
        public void copyCosts(MoveCostTable initTable)
        {
            int i = 0;
            foreach (Object cost in initTable.getTable())
            {
                costs[i] = (int)cost;
                i++;
            }
        }

    }
}

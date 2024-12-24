using ECommons.Configuration;
using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stylist.Configuration;
public class Config : IEzConfig
{
    public Dictionary<Job, Dictionary<BaseParamEnum, float>> Priorities = [];
    public bool UseInventory = true;
    public bool Reequip = true;
    public HashSet<uint> NotifyTerr = [];
}

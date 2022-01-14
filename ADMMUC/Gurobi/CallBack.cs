using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.Diagnostics;
namespace ADMMUC.Solutions;
class CallBackGurobi : GRBCallback
{

    protected PowerSystem PS;
    public List<(double, double, double)> SnapshotUpperBound = new List<(double, double, double)>();
    public GRBVar GenerationCost;
    public GRBVar CycleCost;
    public GRBVar LOLCost;
    public CallBackGurobi(PowerSystem pS, GRBVar generationCost, GRBVar cycleCost, GRBVar lOLCost)
    {
        PS = pS;
        GenerationCost = generationCost;
        CycleCost = cycleCost;
        LOLCost = lOLCost;
        sw.Start();
    }

    Stopwatch sw = new Stopwatch();
    protected override void Callback()
    {
        if (where == GRB.Callback.MIPNODE)
        {
            SnapshotUpperBound.Add((GetDoubleInfo(GRB.Callback.MIPNODE_OBJBST), GetDoubleInfo(GRB.Callback.MIPNODE_OBJBND), (sw.Elapsed.TotalMilliseconds / 1000)));
        }
        if (where == GRB.Callback.MIPSOL)
        {
            SnapshotUpperBound.Add((GetDoubleInfo(GRB.Callback.MIPSOL_OBJ), GetDoubleInfo(GRB.Callback.MIPSOL_OBJBND), (sw.Elapsed.TotalMilliseconds / 1000)));
        }
    }

}




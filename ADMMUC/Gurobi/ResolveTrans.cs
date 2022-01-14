using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.Diagnostics;
namespace ADMMUC.Solutions
{
    class ResolveTrans
    {
        readonly private GRBEnv env = new GRBEnv();
        PowerSystem PS;

        GRBModel Model;
        public GRBVar[,] P; // time x units
        public GRBVar[,] RESDispatch; // time x resunits
        public GRBVar[,] Commit; // time x units
        public GRBVar[,] Start; // time x units
        public GRBVar[,] Stop; // time x units
        public GRBVar[,] NodalLossOfLoad; //  time
        public GRBVar[,] NodalExcessfLoad;//
        public GRBVar[,] TransmissionFlowAC; // lines x time
        public GRBVar[,] NodalInjectionAC; // node x time
        protected int totalNodes;
        protected int totalTime;
        protected int totalUnits;
        protected int totalLinesAC;
        protected int totalRES;
        readonly bool RESOLVE = true;


        public void Kill() { Model.Dispose(); }
        public ResolveTrans(PowerSystem ps, int totalTime, bool forResolve, bool forCheck)
        {
            RESOLVE = forResolve;
            PS = ps;
            Model = new GRBModel(env);
            //Model.Set("OutputFlag", "0");
            this.totalTime = totalTime;
            totalUnits = PS.Units.Count;
            totalNodes = PS.Nodes.Count;
            totalRES = PS.Res.Count;
            totalLinesAC = PS.Lines.Count;
            // totalLinesDC = PS.LinesDC.Count;

            Console.WriteLine("IntialiseVariables");
            IntialiseVariables();
            Console.WriteLine("AddObjective");
            AddObjective();
            Console.WriteLine("AddingGenConstraints");
            AddGenerationConstraint();
            AddLogicConstraint();
            Console.WriteLine("AddingBalanceConstraints");
            AddBalanceConstraint();
            Console.WriteLine("AddRampingConstraints");
            AddRampingConstraints();
            AddTransmissionConstraints();
            AddMinConstraint();
            if (forResolve)
            {
                Console.WriteLine("XD");
                Setup();
                if (forCheck)
                {
                    Setup2();
                }
            }


        }


        public double MIPGAP = 0;
        public List<(double, double, double)> Solve(int timeLimit)
        {
            Console.WriteLine("Solving...");
            Model.Parameters.TimeLimit = timeLimit;
            Model.Parameters.MIPGap = 0.0000000000000001;
            Model.Parameters.Threads = 1;
            var sw = new Stopwatch();
            sw.Start();
            var cb = new CallBackGurobi(PS, GenerationCost, CycleCost, LOLCost, Model);
            Model.SetCallback(cb);
            Model.Optimize();
            sw.Stop();
            MIPGAP = GLOBAL.RelaxGurobi ? 0 : Model.MIPGap;
            // Console.ReadLine();
            //Model.Dispose();
            double LOL = 0;
            for (int t = 0; t < totalTime; t++)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    LOL += NodalLossOfLoad[n, t].X + NodalExcessfLoad[n, t].X;
                }
            }
            Console.WriteLine(LOL);
            cb.SnapshotUpperBound.Add((CurrentObjective.Value, Model.ObjBound, sw.Elapsed.TotalSeconds));
            return cb.SnapshotUpperBound;
        }
        public (double, double, double, double) Solve(int[,] commitStatus)
        {

            SetCommit(commitStatus);
            var sw = new Stopwatch();
            sw.Start();
            Model.Optimize();
            // Console.WriteLine(Model.Status);
            Model.Set("OutputFlag", "0");
            if (Model.Status == GRB.Status.INFEASIBLE || Model.Status == GRB.Status.INF_OR_UNBD)
            {
                Console.WriteLine("Infeasible");
                Model.ComputeIIS();
                string filename = @"C:\Users\Rogier\Desktop\gurobi.ilp";
                Model.Write(filename);
                Process myProcess = new Process();
                Process.Start("notepad++.exe", filename);
                Console.ReadLine();
            }
            double LOL = 0;
            for (int t = 0; t < totalTime; t++)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    LOL += NodalLossOfLoad[n, t].X + NodalExcessfLoad[n, t].X;
                }
            }
            Console.WriteLine("###################################################################");
            //  Console.ReadLine();
            Console.WriteLine("{0} {1} {2}", CurrentObjective.Value, CycleCost.X + GenerationCost.X, LOL);
            Console.WriteLine("###################################################################");
            return (CurrentObjective.Value, sw.Elapsed.TotalMilliseconds, CycleCost.X + GenerationCost.X, LOL);
        }

        internal void KILL()
        {
            Model.Dispose();
        }

        public void Check(int[,] commitStatus, double[,] Ps)
        {


            SetCommit(commitStatus);
            SetP(Ps);
            var sw = new Stopwatch();
            sw.Start();
            Model.Optimize();
            Console.WriteLine(Model.Status);

            if (Model.Status == GRB.Status.INFEASIBLE || Model.Status == GRB.Status.INF_OR_UNBD)
            {
                Console.WriteLine("Infeasible");
                Model.ComputeIIS();
                string filename = @"C:\Users\Rogier\Desktop\gurobi.ilp";
                Model.Write(filename);
                Process myProcess = new Process();
                Process.Start("notepad++.exe", filename);
                throw new Exception();
            }
            //  Console.ReadLine();
        }

        public void IntialiseVariables()
        {

            AddDispatchVariables();
            AddRESDispatch();
            AddBinaryVariables();
            AddNodalVariables();
            AddTransmissionVariables();
        }

        private void AddTransmissionVariables()
        {
            TransmissionFlowAC = new GRBVar[totalLinesAC, totalTime];
            // TransmissionFlowDC = new GRBVar[totalLinesDC, totalTime];
            //NodeVoltAngle = new GRBVar[totalNodes, totalTime];
            for (int l = 0; l < totalLinesAC; l++)
            {
                var line = PS.Lines[l];
                for (int t = 0; t < totalTime; t++)
                {
                    TransmissionFlowAC[l, t] = Model.AddVar(line.MinCapacity, line.MaxCapacity, 0.0, GRB.CONTINUOUS, "TransAC_" + l + "_" + t);
                }
            }
            //for (int l = 0; l < totalLinesDC; l++)
            //{
            //    var line = PS.LinesDC[l];
            //    for (int t = 0; t < totalTime; t++)
            //    {
            //        TransmissionFlowDC[l, t] = Model.AddVar(line.MinCapacity, line.MaxCapacity, 0.0, GRB.CONTINUOUS, "TransDC_" + l + "_" + t);
            //    }
            //}
            //for (int n = 0; n < totalNodes; n++)
            //{
            //    for (int t = 0; t < totalTime; t++)
            //    {
            //        NodeVoltAngle[n, t] = Model.AddVar(double.MinValue, double.MaxValue, 0.0, GRB.CONTINUOUS, "NodeVoltAngle_" + n + "_" + t);
            //    }
            //}
        }
        private void AddTransmissionConstraints()
        {
            for (int t = 0; t < totalTime; t++)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    GRBLinExpr flowInMinusFlowOut = new GRBLinExpr();
                    var node = PS.Nodes[n];
                    for (int l = 0; l < totalLinesAC; l++)
                    {
                        var line = PS.Lines[l];
                        if (node == line.From)
                        {
                            flowInMinusFlowOut -= TransmissionFlowAC[l, t];
                        }
                        if (node == line.To)
                        {
                            flowInMinusFlowOut += TransmissionFlowAC[l, t];
                        }
                    }
                    Model.AddConstr(flowInMinusFlowOut == NodalInjectionAC[n, t], "KirchoffLaw" + n + "t" + t);
                }
            }
        }
        GRBConstr[,] ForcedCommit;

        public void Setup()
        {
            ForcedCommit = new GRBConstr[totalTime, totalUnits];
            for (int t = 0; t < totalTime; t++)
            {
                for (int g = 0; g < totalUnits; g++)
                {
                    ForcedCommit[t, g] = Model.AddConstr(Commit[t, g] == 0, "Forced_" + t + "_" + g);
                }
            }
        }
        GRBConstr[,] ForcedP;
        public void Setup2()
        {
            ForcedP = new GRBConstr[totalTime, totalUnits];
            for (int t = 0; t < totalTime; t++)
            {
                for (int g = 0; g < totalUnits; g++)
                {
                    ForcedP[t, g] = Model.AddConstr(P[t, g] == 0, "ForcedP_" + t + "_" + g);
                }
            }
        }

        public void SetCommit(int[,] givenCommitStatus)
        {
            for (int t = 0; t < totalTime; t++)
            {
                for (int g = 0; g < totalUnits; g++)
                {
                    ForcedCommit[t, g].RHS = givenCommitStatus[t, g];
                }
            }
        }
        public void SetP(double[,] givenPStatus)
        {
            for (int t = 0; t < totalTime; t++)
            {
                for (int g = 0; g < totalUnits; g++)
                {
                    ForcedP[t, g].RHS = givenPStatus[t, g];
                }
            }
        }




        private void AddDispatchVariables()
        {
            P = new GRBVar[totalTime, PS.Units.Count];
            ApplyFunction((t, u) =>
            {
                P[t, u] = Model.AddVar(0, double.MaxValue, 0.0, GRB.CONTINUOUS, "P_" + u + "_" + t);
            });
        }


        private void AddRESDispatch()
        {

            RESDispatch = new GRBVar[totalTime, totalRES];
            for (int t = 0; t < totalTime; t++)
            {
                for (int r = 0; r < totalRES; r++)
                {
                    var RES = PS.Res[r];
                    RESDispatch[t, r] = Model.AddVar(0, RES.GetResValue(t), 0.0, GRB.CONTINUOUS, "RES_" + t + "_" + r);
                }
            }
        }

        public void AddBinaryVariables()
        {
            Commit = new GRBVar[totalTime, PS.Units.Count];
            Start = new GRBVar[totalTime, PS.Units.Count];
            Stop = new GRBVar[totalTime, PS.Units.Count];
            ApplyFunction((t, u) =>
            {
                Unit unit = PS.Units[u];
                Commit[t, u] = Model.AddVar(0.0, 1, 0.0, RESOLVE || GLOBAL.RelaxGurobi ? GRB.CONTINUOUS : GRB.BINARY, "Commit_" + t + "_" + u);
                Start[t, u] = Model.AddVar(0.0, 1, 0.0, RESOLVE || GLOBAL.RelaxGurobi ? GRB.CONTINUOUS : GRB.BINARY, "Start_" + t + "_" + u);
                Stop[t, u] = Model.AddVar(0.0, 1, 0.0, RESOLVE || GLOBAL.RelaxGurobi ? GRB.CONTINUOUS : GRB.BINARY, "Stop_" + t + "_" + u);
            });

        }

        private void AddNodalVariables()
        {
            NodalLossOfLoad = new GRBVar[totalNodes, totalTime];
            NodalExcessfLoad = new GRBVar[totalNodes, totalTime];
            NodalInjectionAC = new GRBVar[totalNodes, totalTime];
            for (int n = 0; n < totalNodes; n++)
                for (int t = 0; t < totalTime; t++)
                {
                    NodalInjectionAC[n, t] = Model.AddVar(double.MinValue, double.MaxValue, 0.0, GRB.CONTINUOUS, "NodalInjectionAC_" + t);
                    NodalLossOfLoad[n, t] = Model.AddVar(0, double.MaxValue, 0.0, GRB.CONTINUOUS, "NodalLoL_" + t);
                    NodalExcessfLoad[n, t] = Model.AddVar(0, double.MaxValue, 0.0, GRB.CONTINUOUS, "NodalLeL_" + t);
                }

        }

        public void ApplyFunction(Action<int, int> action)
        {
            for (int t = 0; t < totalTime; t++)
            {
                for (int u = 0; u < totalUnits; u++)
                {
                    action(t, u);
                }
            }
        }
        
        public GRBLinExpr CurrentObjective;
        public GRBVar GenerationCost;
        public GRBVar CycleCost;
        public GRBVar LOLCost;

        public void AddObjective()
        {
            CurrentObjective = 0;
            LinkGenerationCost();
            LinkStartUpCost();
            LinkLossOfLoad();
            //Objective += GenerationCostVariable + CycleCostVariable + LOLCostVariable;
            CurrentObjective += GenerationCost + CycleCost + LOLCost;

            Model.SetObjective(CurrentObjective, GRB.MINIMIZE);
        }

        private void LinkLossOfLoad()
        {
            LOLCost = Model.AddVar(double.MinValue, double.MaxValue, 0.0, GRB.CONTINUOUS, "LOLCostVariable");
            GRBLinExpr lolCost = new GRBLinExpr();
            for (int n = 0; n < totalNodes; n++)
                for (int t = 0; t < totalTime; t++)
                {
                    lolCost += NodalLossOfLoad[n, t] * PS.VOLL + NodalExcessfLoad[n, t] * PS.VOLL;

                }
            Model.AddConstr(LOLCost == lolCost, "");
        }


        private void LinkGenerationCost()
        {
            GenerationCost = Model.AddVar(double.MinValue, double.MaxValue, 0.0, GRB.CONTINUOUS, "GenerationCost");
            GRBQuadExpr GenCost = 0;
            for (int t = 0; t < totalTime; t++)
            {
                GRBQuadExpr GenCostTime = 0;
                Console.Write(t);
                for (int u = 0; u < totalUnits; u++)
                {
                    Unit unit = PS.Units[u];
                    //Console.WriteLine(Vars.PiecewiseGeneration[u]);
                    GenCostTime += Commit[t, u] * unit.A;
                    GenCostTime += P[t, u] * unit.B + (unit.C != 0 ? P[t, u] * P[t, u] * unit.C : 0);
                    if (unit.C != 0)
                        GenCostTime -= 1;
                }
                GenCost += GenCostTime;
            }
            Model.AddQConstr(GenerationCost == GenCost, "");
        }

        public void LinkStartUpCost()
        {
            CycleCost = Model.AddVar(double.MinValue, double.MaxValue, 0.0, GRB.CONTINUOUS, "CycleCostVariable");
            GRBLinExpr cycleCost = new GRBLinExpr();
            ApplyFunction((t, u) =>
            {
                Unit unit = PS.Units[u];
                cycleCost += unit.StartCostInterval.First() * Start[t, u];
            });
            Model.AddConstr(CycleCost == cycleCost, "");
        }

        protected void ForEachTimeStepAndGenerator(Action<int, int> action)
        {
            for (int t = 0; t < totalTime; t++)
            {
                for (int u = 0; u < totalUnits; u++)
                {
                    action(t, u);
                }
            }
        }
        protected void ForEachTimeStepAndGenerator(Action<int, int> action, int start, int stop)
        {
            for (int t = start; t < stop; t++)
            {
                for (int u = 0; u < totalUnits; u++)
                {
                    action(t, u);
                }
            }
        }

        public void AddGenerationConstraint()
        {

            ForEachTimeStepAndGenerator((t, u) => AddNormalConstraint(t, u));
        }

        private void AddNormalConstraint(int t, int u)
        {
            Unit unit = PS.Units[u];
            GRBLinExpr maxGeneration = (unit.PMax) * Commit[t, u];
            Model.AddConstr(P[t, u] <= maxGeneration, "MaxGeneration_" + t + "_" + u);
            Model.AddConstr(P[t, u] >= (unit.PMin) * Commit[t, u], "MinGeneration_" + t + "_" + u);
        }



        public void AddLogicConstraint()
        {
            ForEachTimeStepAndGenerator((t, u) => AddLogicConstraint(t, u), 1, totalTime);
        }
        public void AddLogicConstraint(int t, int u)
        {

            var PowerPlantLogic = Commit[t - 1, u] - Commit[t, u] + Start[t, u] - Stop[t, u] == 0;
            Model.AddConstr(PowerPlantLogic, "Power Plant Logic" + t + " " + u);
        }

        GRBConstr[] NodalPowerBalance;
        public GRBVar[] NodalResidualDemand;

        public void AddBalanceConstraint()
        {

            NodalPowerBalance = new GRBConstr[totalTime];
            NodalResidualDemand = new GRBVar[totalTime];
            for (int n = 0; n < totalNodes; n++)
                for (int t = 0; t < totalTime; t++)
                {
                    AddPowerBalanceConstraint(n, t);
                }
        }
        private void AddPowerBalanceConstraint(int n, int t)
        {
            GRBLinExpr generation = new GRBLinExpr();
            generation += NodalGeneration(n, t);
            generation += NodalResGeneration(n, t);
            generation += NodalInjectionAC[n, t];
            generation += NodalLossOfLoad[n, t];

            GRBLinExpr consumption = new GRBLinExpr();
            consumption += PS.Nodes[n].NodalDemand(t) + NodalExcessfLoad[n, t];

            NodalPowerBalance[t] = Model.AddConstr(generation == consumption, "NodalPowerBalance" + t);
        }

        private GRBLinExpr NodalGeneration(int n, int t)
        {
            var totalGeneration = new GRBLinExpr();
            PS.Nodes[n].UnitsIndex.Take(PS.Nodes[n].UnitsIndex.Count() - (RESOLVE ? 1 : 0)).ToList().ForEach(g =>
                 {
                     totalGeneration += P[t, g];
                 });
            return totalGeneration;
        }
        private GRBLinExpr NodalResGeneration(int n, int t)
        {
            GRBLinExpr ResGeneration = new GRBLinExpr();
            PS.Nodes[n].RESindex.ForEach(r =>
            {
                ResGeneration += RESDispatch[t, r];
            });
            return ResGeneration;
        }


        protected GRBConstr[,] UpwardRampingConstr;
        protected GRBConstr[,] DownwardRampingConstr;
        public void AddRampingConstraints()
        {

            UpwardRampingConstr = new GRBConstr[totalTime, totalUnits];
            DownwardRampingConstr = new GRBConstr[totalTime, totalUnits];
            ForEachTimeStepAndGenerator((t, u) => AddRampingConstraint(t, u), 1, totalTime);

        }

        public void AddRampingConstraint(int t, int u)
        {
            Unit unit = PS.Units[u];

            var upwardRampingLimitNormal = unit.RampUp * Commit[t, u];
            var upwardRampingLimitStartup = (unit.StartUp - unit.RampUp) * Start[t, u];
            var upwardRampingLimit = upwardRampingLimitNormal + upwardRampingLimitStartup;
            // Console.ReadLine();
            //Console.WriteLine("check dit even");
            UpwardRampingConstr[t, u] = Model.AddConstr(P[t, u] - P[t - 1, u] <= upwardRampingLimit, "rampup_" + t + "_" + u);
            // UpwardRampingConstr[t, u] = Model.AddConstr(Variable.PotentialP[t, u] - Variable.P[t - 1, u] <= upwardRampingLimit, "r" + u + "t" + t);

            var downwardRampingLimitNormal = unit.RampDown * Commit[t - 1, u];
            var downwardRampingLimitShutdown = Stop[t, u] * (unit.ShutDown - unit.RampDown);
            var downwardRampingLimit = downwardRampingLimitNormal + downwardRampingLimitShutdown;
            DownwardRampingConstr[t, u] = Model.AddConstr(P[t - 1, u] - P[t, u] <= downwardRampingLimit, "rampdown_" + t + "_" + u);
        }

        public void AddMinConstraint()
        {

            ForEachTimeStepAndGenerator((t, u) => AddMinimumUpTime(t, u));
            ForEachTimeStepAndGenerator((t, u) => AddMinimumDownTime(t, u));

        }

        private void AddMinimumUpTime(int t, int u)
        {
            var unit = PS.Units[u];
            var amountOfTimeStartedInPeriod = new GRBLinExpr();
            int maxLookBack = Math.Max(0, t - unit.MinUpTime);
            for (int t2 = t; t2 > maxLookBack; t2--)
            {
                amountOfTimeStartedInPeriod += Start[t2, u];
            }
            Model.AddConstr(Commit[t, u] >= amountOfTimeStartedInPeriod, "MinUpTime" + t + "u" + u);
        }

        private void AddMinimumDownTime(int t, int u)
        {
            var unit = PS.Units[u];
            var amountOfTimeStoppedInPeriod = new GRBLinExpr();
            int maxLookBack = Math.Max(0, t - unit.MinDownTime);
            for (int t2 = t; t2 > maxLookBack; t2--)
            {
                amountOfTimeStoppedInPeriod += Stop[t2, u];
            }
            Model.AddConstr(1 - Commit[t, u] >= amountOfTimeStoppedInPeriod, "MinDownTime" + t + "u" + u);
        }
        GRBConstr[,] PDTFConstraints;
        private void AddPDTFConstraints()
        {
            PDTFConstraints = new GRBConstr[totalTime, totalLinesAC];
            for (int t = 0; t < totalTime; t++)
            {
                for (int l = 0; l < totalLinesAC; l++)
                {

                    GRBLinExpr trans = new GRBLinExpr();
                    //n=1 instead of n=0 because we skip the reference node and we assume the first node is the reference node
                    for (int n = 1; n < totalNodes; n++)
                    {
                        trans += PS.PTDF[l, n] * NodalInjectionAC[n, t];
                    }
                    PDTFConstraints[t, l] = Model.AddConstr(TransmissionFlowAC[l, t] == trans, "PDTF" + t + "l" + l);
                }
            }
;
        }
    }
}




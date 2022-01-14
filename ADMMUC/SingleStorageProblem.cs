using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace ADMMUC
{
    class SingleStorageProblem
    {
        StorageUnit StorageUnit;
        private GRBEnv env;
        GRBModel Model;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SingleStorageProblem(StorageUnit storageUnit)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            StorageUnit = storageUnit;
            CreateGurobiEnviorment();
        }
        private void CreateGurobiEnviorment()
        {
            env = new GRBEnv();
        }

        public double SolveForMultipliers(List<double> Multipliers, double beginLevel, double endLevel)
        {
            Model = new GRBModel(env);
            int totalTime = Multipliers.Count();
            var Storage = new GRBVar[totalTime];
            var Charge = new GRBVar[totalTime];
            var Discharge = new GRBVar[totalTime];

            for (int t = 0; t < totalTime; t++)
            {
                Storage[t] = Model.AddVar(0, StorageUnit.MaxEnergy, 0.0, GRB.CONTINUOUS, "t" + t);
                Charge[t] = Model.AddVar(0, StorageUnit.MaxCharge, 0.0, GRB.CONTINUOUS, "Charge" + t);
                Discharge[t] = Model.AddVar(0, StorageUnit.MaxDischarge, 0.0, GRB.CONTINUOUS, "Discharge" + t);
            }
            GRBLinExpr objective = new GRBLinExpr();
            for (int t = 0; t < totalTime; t++)
            {

                if (t == 0)
                {
                    Model.AddConstr(Storage[0] == beginLevel + Charge[0] * StorageUnit.ChargeEffiency - Discharge[0] * StorageUnit.DischargeEffiencyInverse, "InitalStorageLevel");
                }
                else
                {
                    Model.AddConstr(Storage[t] == Storage[t - 1] + Charge[t] * StorageUnit.ChargeEffiency - Discharge[t] * StorageUnit.DischargeEffiencyInverse, "StorageLevel" + t);
                }
                objective += Charge[t] * Multipliers[t] + Discharge[t] * -Multipliers[t];
            }
            Model.AddConstr(Storage[totalTime - 1] == endLevel, "");
            Model.SetObjective(objective);
            Model.Optimize();
            return objective.Value;
        }
    }
}

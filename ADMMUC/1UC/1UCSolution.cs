using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC._1UC
{
   public  class SUCSolution
    {
        public List<DPQSolution> Steps;
        public double GenerationCostOnly;
        public double CostADMM;
        //public double CostLR;

        public SUCSolution(SUC UC, List<DPQSolution> steps, double costADMM)
        {
            Steps = steps;
            CostADMM = costADMM;
            GenerationCostOnly = ReevalSolution(UC);
         //   CostLR = ReevalSolutionLR(UC);
        }
        private double ReevalSolution(SUC UC)
        {
            double startCost = Steps.Skip(1).Where(step => step.On && step.Tau == 0).Sum(step => UC.startCost);
            double generationCost = Steps.Sum(step => (step.On ? UC.A : 0) + UC.B * step.P + UC.C * step.P * step.P);
            return startCost + generationCost;
        }
        private double ReevalSolutionLR(SUC UC)
        {
            double startCost = Steps.Skip(1).Where(step => step.On && step.Tau == 0).Sum(step => UC.startCost);
            double LRGenerationCost = 0;
            for (int t = 0; t < UC.LagrangeMultipliers.Count; t++)
            {
                var step = Steps[t];
                LRGenerationCost += (step.On ? UC.A : 0) + (UC.B + UC.LagrangeMultipliers[t]) * step.P + UC.C * step.P * step.P;
            }
            return startCost + LRGenerationCost;
        }
    }
}

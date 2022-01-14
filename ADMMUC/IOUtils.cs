using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.IO;
using ADMMUC.PWS;
using System.Xml.Serialization;

namespace ADMMUC;

static class IOUtils
{

    internal static PowerSystem GetPowerSystem(string filenameInstance, ConstraintConfiguration CC)
    {
        var lines = File.ReadAllLines(filenameInstance).ToList();
        var units = ParseUnits(CC, GetLineInterval("units", lines).Skip(1).ToList());
        var demandString = GetLineInterval("demands", lines)[1].Split(';')[2];
        var resGeneration = ParseRESgeneration(GetLineInterval("RESgeneration", lines).Skip(1).ToList());
        var nodes = ParseNodes(GetLineInterval("nodes", lines).Skip(1).ToList());
        ParseDemand(nodes, GetLineInterval("demands", lines).Skip(1).ToList());
        var transmissionLines = ParseLines(GetLineInterval("transmissionAC", lines).Skip(1).ToList(), nodes);
        var inflows = ParseInflows(GetLineInterval("inflows", lines).Skip(1).ToList(), int.MaxValue);
        var storageUnits = ParseStorage(GetLineInterval("storage", lines).Skip(1).ToList(), inflows);
        var PS = new PowerSystem(filenameInstance.Split('\\').Last(), units, nodes, transmissionLines, resGeneration, storageUnits, CC);
        nodes.ForEach(node => node.UnitsIndex.ForEach(uID => units[uID].NodeID = node.ID));
        return PS;
    }
    private static List<ResGeneration> ParseRESgeneration(List<string> lines)
    {
        List<ResGeneration> resgen = new List<ResGeneration>();

        foreach (var line in lines)
        {
            var output = line.Split(';');
            int ID = int.Parse(output[0]);
            string name = output[1];
            List<double> values = GetValues(output[2]).Select(cell => double.Parse(cell)).ToList();

            resgen.Add(new ResGeneration(ID, values, name));
        }
        return resgen;
    }
    static public List<Node> ParseNodes(List<string> lines)
    {
        List<Node> nodes = new List<Node>();
        foreach (var line in lines)
        {
            var input = line.Split(';');
            int id = int.Parse(input[0]);
            string name = input[1];

            var unitIndices = GetValues(input[2]).Select(index => int.Parse(index)).ToList();
            var storageIndices = GetValues(input[3]).Select(index => int.Parse(index)).ToList();
            var RESIndices = GetValues(input[4]).Select(index => int.Parse(index)).ToList();
            var node = new Node(id, name, unitIndices, storageIndices, RESIndices);
            nodes.Add(node);
        }
        return nodes;
    }
    static private List<string> GetValues(string line)
    {
        if (line.Length == 2) return new List<string>();
        return line[1..^1].Split(':').ToList();
    }
    static public List<TransmissionLine> ParseLines(List<string> lines, List<Node> Nodes)
    {
        List<TransmissionLine> transmissionLines = lines.Select(line =>
        {

            var cells = line.Split(';');
            int IDFrom = int.Parse(cells[0]);
            int IDTo = int.Parse(cells[1]);
            double capacity = double.Parse(cells[2]);
            double susceptance = double.Parse(cells[3]);
            Node From = Nodes[IDFrom];
            Node To = Nodes[IDTo];
            return new TransmissionLine(From, To, -capacity, capacity, susceptance);
        }).ToList();
        return transmissionLines;
    }
    static public List<StorageUnit> ParseStorage(List<string> lines, List<Inflow> inflows)
    {
        List<StorageUnit> storageUnits = new List<StorageUnit>();
        foreach (var line in lines)
        {
            var input = line.Split(';');
            int i = 0;
            var id = (input[i++]);
            string name = input[i++];
            double maxCharge = double.Parse(input[i++]);
            double maxDischarge = double.Parse(input[i++]);
            double maxEnergy = double.Parse(input[i++]);
            double chargeEffiency = double.Parse(input[i++]);
            double dischargeEffiency = double.Parse(input[i++]);
            var inflow = inflows.Where(flow => flow.StorageID == id);
            if (inflow.Any())
            {
                var storageUnit = new StorageUnit(id, name, maxCharge, maxDischarge, maxEnergy, chargeEffiency, dischargeEffiency, inflow.First().Inflows);
                storageUnits.Add(storageUnit);
            }
            else
            {
                var storageUnit = new StorageUnit(id, name, maxCharge, maxDischarge, maxEnergy, chargeEffiency, dischargeEffiency, new List<double>());
                storageUnits.Add(storageUnit);
            }
        }
        return storageUnits;
    }

    private static void ParseDemand(List<Node> nodes, List<string> lines)
    {
        foreach (var line in lines)
        {
            var input = line.Split(';');
            int ID = int.Parse(input[0]);
            int NodeID = int.Parse(input[1]);
            var values = GetValues(input[2]).Select(v => double.Parse(v)).ToList();
            nodes[NodeID].SetDemand(values);
        }
    }

    static public List<Unit> ParseUnits(ConstraintConfiguration CC, List<string> lines)
    {
        List<Unit> units = new List<Unit>();

        foreach (var line in lines.Take(CC.maxUnits))
        {
            var input = line.Split(';');
            int i = 0;
            int id = int.Parse(input[i++]);
            int count = int.Parse(input[i++]);

            var unit = new Unit(id, count);

            //string name = input[i++];
            double pMin = double.Parse(input[i++]);
            double pMax = double.Parse(input[i++]);
            unit.SetGenerationLimits(pMin, pMax);

            double a = double.Parse(input[i++]);
            double b = double.Parse(input[i++]);
            double c = double.Parse(input[i++]);
            unit.SetGenerationCost(a, b, c);

            double rampUp = double.Parse(input[i++]);
            double rampDown = double.Parse(input[i++]);
            double startUp = double.Parse(input[i++]);
            double shutdown = double.Parse(input[i++]);
            unit.SetRampLimits(rampUp, rampDown, startUp, shutdown);

            int minUpTime = int.Parse(input[i++]);
            int minDownTime = int.Parse(input[i++]);
            unit.SetMinTime(minDownTime, minUpTime, CC.MinUpMinDown);

            double FSC = double.Parse(input[i++]);
            double VSC = double.Parse(input[i++]);
            double lambda = double.Parse(input[i++]);
            bool parseStartupCostAsFunction = FSC == -1 && VSC == -1 && lambda == -1;

            //if the time dependant startup costs is defined as a function instead of a discretised step function,
            //skip those values
            if (!parseStartupCostAsFunction)
            {
                i++; i++;
                unit.SetSUFunction(FSC, VSC, lambda);
            }
            else
            {
                double[] startCostInterval = input[i++].Split(':').Select(cost => double.Parse(cost)).ToArray();
                int[] startInterval = input[i++].Split(':').Select(interval => int.Parse(interval)).ToArray();
                unit.SetSUInterval(startCostInterval, startInterval);
            }
            unit.CreateUniformPiecewiseFunction(CC.PiecewiseSegments);
            unit.Fix();
            units.Add(unit);

        }

        return units;
    }


    static public List<Inflow> ParseInflows(List<string> lines, int timeStepLimit)
    {
        List<Inflow> inflows = new List<Inflow>();
        foreach (var line in lines)
        {
            var input = line.Split(';');
            int id = int.Parse(input[0]);
            var StorageID = (input[1]);
            var values = GetValues(input[2]).Select(value => double.Parse(value)).Take(timeStepLimit).ToList();
            inflows.Add(new Inflow(id, StorageID, values));
        }
        return inflows;
    }


    static public List<string> GetLineInterval(string identifier, List<string> lines)
    {
        string begin = "<" + identifier + ">";
        string end = "</" + identifier + ">";

        bool skip = true;

        List<string> newLines = new List<string>();
        foreach (var line in lines)
        {
            if (end == line)
            {
                skip = true;
            }
            if (!skip)
            {
                newLines.Add(line);
            }
            if (begin == line)
            {
                skip = false;
            }
        }
        return newLines;
    }
}

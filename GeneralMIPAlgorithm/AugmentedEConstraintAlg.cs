﻿using System;
using System.Collections;
using System.Text;
using System.IO;

namespace GeneralMIPAlgorithm
{
    public struct ParetoPoints 
    {
        public double sumDesire;
        public double minDesire;
        public double resDemand;
        public double emrDemand;
        public double minDemand;
        public double regDemand;

        public string displayMe() {
            string disp = "";
            disp += sumDesire + "\t" + minDesire + "\t" + resDemand + "\t" + emrDemand + "\t" + minDemand + "\t" + regDemand;

            return disp;
        }

    }
    public class AugmentedEConstraintAlg
    {
        public int totalObjective;
        public double[][] payOffTable;
        public double[] minRange_o;
        public double[] maxRange_o;
        public int[] rangeQ_o;
        DataLayer.AllData data;
        public ArrayList eConstraitn;
        public double[][] rangeInterval_oi;
        public ArrayList paretoSol;
        public int augCounter;


        public AugmentedEConstraintAlg(DataLayer.AllData alldata, string InsName) {
            initial(alldata);
            setAUGMECONSpayoff(InsName);
            setAUGMECONSRange(InsName);
            setAUGMECONSPareto(InsName);
        }

        public void initial(DataLayer.AllData alldata)
        {
            totalObjective = 6;
            payOffTable = new double[totalObjective][];
            for (int o = 0; o < totalObjective; o++)
            {
                payOffTable[o] = new double[totalObjective];
                for (int oo = 0; oo < totalObjective; oo++)
                {
                    payOffTable[o][oo] = 0;
                }
            }
            minRange_o = new double[totalObjective];
            maxRange_o = new double[totalObjective];
            rangeQ_o = new int[totalObjective];
            for (int o = 0; o < totalObjective; o++)
            {
                minRange_o[o] = 0;
                maxRange_o[o] = 0;
                rangeQ_o[o] = 0;

            }
            rangeQ_o = new int[] { 3, 3, 2, 2, 2, 2 };
            data = alldata;
            eConstraitn = new ArrayList();
            paretoSol = new ArrayList();
            augCounter = 0;
        }

        public void setAUGMECONSpayoff(string InsName)
        {
            string name = InsName + "AugID_" + augCounter;
            for (int o = 0; o < totalObjective; o++)
            {
                augCounter++;
                AUGMECONS root = new AUGMECONS(totalObjective);
                for (int oo = 0; oo < totalObjective; oo++)
                {
                    root.activeObj_o[oo] = true;
                    root.priority_o[oo] = totalObjective - oo;
                    if (o == oo)
                    {
                        root.priority_o[oo] = totalObjective + 1;// higher priority
                    }
                }
                
                // solve the problem find the objective
                MedicalTraineeSchedulingMIP mip = new MedicalTraineeSchedulingMIP(data, root, true, name);
                if (!mip.notFeasible)
                {
                    for (int oo = 0; oo < totalObjective; oo++)
                    {
                        payOffTable[o][oo] = mip.multiObjectiveValue[oo];
                        if (minRange_o[oo] > payOffTable[o][oo])
                        {
                            minRange_o[oo] = payOffTable[o][oo];
                        }
                    }
                    paretoSol.Add(new ParetoPoints
                    {
                        sumDesire = payOffTable[o][0],
                        minDesire = payOffTable[o][1],
                        resDemand = payOffTable[o][2],
                        emrDemand = payOffTable[o][3],
                        minDemand = payOffTable[o][4],
                        regDemand = payOffTable[o][5],
                    });
                    maxRange_o[o] = payOffTable[o][o];
                }
                
                
            }
        }

        public void setAUGMECONSRange(string InsName) 
        {
            eConstraitn = new ArrayList();
            StreamWriter swEC = new StreamWriter(data.allPath.OutPutGr + "econstGrid.txt");
            int counter = 0;
            for (int mnD = 0; mnD < rangeQ_o[1]; mnD++)
            {
                for (int rsD = 0; rsD < rangeQ_o[2]; rsD++)
                {
                    for (int emD = 0; emD < rangeQ_o[3]; emD++)
                    {
                        for (int miD = 0; miD < rangeQ_o[4]; miD++)
                        {
                            for (int rgD = 0; rgD < rangeQ_o[5]; rgD++)
                            {
                                AUGMECONS tmpAug = new AUGMECONS(totalObjective);
                                counter++;
                                for (int o = 1; o < totalObjective; o++)
                                {
                                    double econst = 0;
                                    switch (o)
                                    {
                                        case 1:
                                            econst = minRange_o[o] + mnD * (maxRange_o[o] - minRange_o[o]) / rangeQ_o[o];
                                            break;
                                        case 2:
                                            econst = minRange_o[o] + rsD * (maxRange_o[o] - minRange_o[o]) / rangeQ_o[o];
                                            break;
                                        case 3:
                                            econst = minRange_o[o] + emD * (maxRange_o[o] - minRange_o[o]) / rangeQ_o[o];
                                            break;
                                        case 4:
                                            econst = minRange_o[o] + miD * (maxRange_o[o] - minRange_o[o]) / rangeQ_o[o];
                                            break;
                                        case 5:
                                            econst = minRange_o[o] + rgD * (maxRange_o[o] - minRange_o[o]) / rangeQ_o[o];
                                            break;
                                        default:
                                            break;
                                    }
                                    tmpAug.upperBound_o[o] = maxRange_o[o];
                                    tmpAug.lowerBound_o[o] = minRange_o[o];
                                    tmpAug.epsilonCon_o[o] = econst;
                                    tmpAug.activeConst_o[o] = true;
                                    tmpAug.activeObj_o[0] = true; // only the first objective is active
                                    tmpAug.objectiveRange_o[o] = maxRange_o[o] - minRange_o[o];

                                }
                                eConstraitn.Add(tmpAug);
                                swEC.WriteLine("==================="+counter+"=====================");
                                swEC.WriteLine(tmpAug.displayMe());
                                
                            }
                        }
                    }
                }
            }

        }

        public void setAUGMECONSPareto(string InsName) 
        {
            
            foreach (AUGMECONS aug in eConstraitn)
            {
                string name = InsName + "AugID_" + augCounter;
                MedicalTraineeSchedulingMIP tmp = new MedicalTraineeSchedulingMIP(data, aug, false, name);
                if (!tmp.notFeasible)
                {
                    paretoSol.Add(new ParetoPoints
                    {
                        sumDesire = tmp.multiObjectiveValue[0],
                        minDesire = tmp.multiObjectiveValue[1],
                        resDemand = tmp.multiObjectiveValue[2],
                        emrDemand = tmp.multiObjectiveValue[3],
                        minDemand = tmp.multiObjectiveValue[4],
                        regDemand = tmp.multiObjectiveValue[5],
                    });
                }
            }
        }
    }
}